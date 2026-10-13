using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blazored.LocalStorage;
using GymTracker.App.Models;

namespace GymTracker.App.Services;

public class AuthService : IAuthService
{
    private const string StorageKey = "gym-tracker-auth";

    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private AuthSession? _session;

    public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public bool IsAuthenticated => _session is not null;
    public string? UserId => _session?.UserId;
    public string? Email => _session?.Email;

    public event Action? AuthStateChanged;

    public async Task InitializeAsync()
    {
        var stored = await _localStorage.GetItemAsync<AuthSession>(StorageKey);
        if (stored is null)
        {
            return;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() < stored.ExpiresAtUnix - 30)
        {
            SetSession(stored);
            return;
        }

        var (refreshed, rejected) = await TryRefreshAsync(stored.RefreshToken);
        if (refreshed is not null)
        {
            SetSession(refreshed);
        }
        else if (rejected)
        {
            // The server explicitly rejected the refresh token - this session is truly gone.
            await _localStorage.RemoveItemAsync(StorageKey);
        }
        else
        {
            // Couldn't reach the server (offline). Keep the stale session so the app still
            // works locally - cloud sync will simply fail silently until connectivity returns.
            SetSession(stored);
        }
    }

    public async Task<string?> SignInAsync(string email, string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "token?grant_type=password")
        {
            Content = JsonContent.Create(new { email, password }),
        };

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return await ExtractErrorMessageAsync(response);
        }

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (token?.User is null)
        {
            return "Sign in failed: unexpected response from server.";
        }

        await PersistTokenAsync(token);
        return null;
    }

    public async Task SignOutAsync()
    {
        if (_session is not null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "logout");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
            try
            {
                await _httpClient.SendAsync(request);
            }
            catch
            {
                // best-effort; we clear local state regardless
            }
        }

        await _localStorage.RemoveItemAsync(StorageKey);
        _session = null;
        AuthStateChanged?.Invoke();
    }

    public async Task<string?> GetValidAccessTokenAsync()
    {
        if (_session is null)
        {
            return null;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() < _session.ExpiresAtUnix - 30)
        {
            return _session.AccessToken;
        }

        var (refreshed, rejected) = await TryRefreshAsync(_session.RefreshToken);
        if (refreshed is not null)
        {
            SetSession(refreshed);
            return refreshed.AccessToken;
        }

        if (rejected)
        {
            await SignOutAsync();
        }

        // Offline, or rejected: either way there's no usable token right now. On an outright
        // rejection we've just signed out above; on a network failure we deliberately leave the
        // stale local session alone so the app keeps working offline, and simply return null so
        // this one sync attempt fails and retries later.
        return null;
    }

    /// <summary>
    /// Attempts a token refresh. The "rejected" flag is only true when the server itself rejected
    /// the refresh token (expired/revoked) - as opposed to the request simply failing to reach the
    /// server at all (offline), which must NOT be treated as a sign-out.
    /// </summary>
    private async Task<(AuthSession? Session, bool Rejected)> TryRefreshAsync(string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "token?grant_type=refresh_token")
        {
            Content = JsonContent.Create(new { refresh_token = refreshToken }),
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch
        {
            return (null, false);
        }

        if (!response.IsSuccessStatusCode)
        {
            return (null, true);
        }

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (token?.User is null)
        {
            return (null, true);
        }

        var session = ToAuthSession(token);
        await _localStorage.SetItemAsync(StorageKey, session);
        return (session, false);
    }

    private async Task PersistTokenAsync(TokenResponse token)
    {
        var session = ToAuthSession(token);
        await _localStorage.SetItemAsync(StorageKey, session);
        SetSession(session);
    }

    private void SetSession(AuthSession session)
    {
        _session = session;
        AuthStateChanged?.Invoke();
    }

    private static AuthSession ToAuthSession(TokenResponse token) => new()
    {
        AccessToken = token.AccessToken,
        RefreshToken = token.RefreshToken,
        ExpiresAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + token.ExpiresIn,
        UserId = token.User!.Id,
        Email = token.User.Email,
    };

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(body);
            foreach (var propertyName in new[] { "msg", "message", "error_description", "error" })
            {
                if (doc.RootElement.TryGetProperty(propertyName, out var value))
                {
                    return value.GetString() ?? body;
                }
            }
        }
        catch (JsonException)
        {
            // fall through to raw body below
        }

        return string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase ?? "Request failed" : body;
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = "";
        [JsonPropertyName("expires_in")] public long ExpiresIn { get; set; }
        [JsonPropertyName("user")] public SupabaseUserDto? User { get; set; }
    }

    private class SupabaseUserDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("email")] public string Email { get; set; } = "";
    }
}
