namespace GymTracker.App.Services;

public interface IAuthService
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? Email { get; }

    event Action? AuthStateChanged;

    Task InitializeAsync();
    Task<string?> SignInAsync(string email, string password);
    Task SignOutAsync();

    /// <summary>Returns a non-expired access token, refreshing first if needed. Null if not signed in or refresh failed.</summary>
    Task<string?> GetValidAccessTokenAsync();
}
