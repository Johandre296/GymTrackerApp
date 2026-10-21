using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GymTracker.App.Models;

namespace GymTracker.App.Services;

public class CloudSyncService : ICloudSyncService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _auth;

    public CloudSyncService(HttpClient httpClient, IAuthService auth)
    {
        _httpClient = httpClient;
        _auth = auth;
    }

    public async Task PushSessionAsync(WorkoutSession session)
    {
        var token = await _auth.GetValidAccessTokenAsync()
            ?? throw new InvalidOperationException("Not signed in.");

        var payload = new
        {
            id = session.Id,
            user_id = _auth.UserId,
            date = session.Date.ToString("yyyy-MM-dd"),
            day_name = session.DayName,
            data = session.Exercises,
            updated_at = session.UpdatedAt,
            notes = session.Notes,
            // Explicit null so an upsert always un-tombstones the row - PostgREST's merge-duplicates
            // only touches columns present in the payload, so omitting this would leave a prior
            // soft-delete in place if the same id/date is ever legitimately re-pushed.
            deleted_at = (DateTime?)null,
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "workout_sessions")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Prefer", "resolution=merge-duplicates,return=minimal");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SyncResult<WorkoutSession, Guid>> PullAllAsync()
    {
        var token = await _auth.GetValidAccessTokenAsync();
        if (token is null)
        {
            return new SyncResult<WorkoutSession, Guid>();
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"workout_sessions?user_id=eq.{_auth.UserId}&select=*");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var rows = await response.Content.ReadFromJsonAsync<List<SupabaseSessionRow>>() ?? new();
        return new SyncResult<WorkoutSession, Guid>
        {
            AllKnownKeys = rows.Select(r => r.Id).ToHashSet(),
            Active = rows
                .Where(r => r.DeletedAt is null)
                .Select(r => new WorkoutSession
                {
                    Id = r.Id,
                    Date = DateOnly.Parse(r.Date),
                    DayName = r.DayName,
                    Exercises = r.Data,
                    UpdatedAt = r.UpdatedAt,
                    Notes = r.Notes ?? string.Empty,
                })
                .ToList(),
        };
    }

    public async Task DeleteSessionAsync(Guid sessionId)
    {
        var token = await _auth.GetValidAccessTokenAsync()
            ?? throw new InvalidOperationException("Not signed in.");

        // Soft delete: a hard DELETE would let a device with a stale local copy "resurrect" this
        // session by pushing it back up during its next sync, since a missing row looks identical to
        // one that was simply never synced. Marking it deleted_at lets sync tell the two apart.
        var request = new HttpRequestMessage(HttpMethod.Patch, $"workout_sessions?id=eq.{sessionId}")
        {
            Content = JsonContent.Create(new { deleted_at = DateTime.UtcNow }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Prefer", "return=minimal");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task PushPersonalBestAsync(PersonalBest personalBest)
    {
        var token = await _auth.GetValidAccessTokenAsync()
            ?? throw new InvalidOperationException("Not signed in.");

        var payload = new
        {
            user_id = _auth.UserId,
            exercise_name = personalBest.ExerciseName,
            weight = personalBest.Weight,
            reps = personalBest.Reps,
            session_id = personalBest.SessionId,
            achieved_at = personalBest.AchievedAt,
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "personal_bests")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Prefer", "resolution=merge-duplicates,return=minimal");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<PersonalBest>> PullAllPersonalBestsAsync()
    {
        var token = await _auth.GetValidAccessTokenAsync();
        if (token is null)
        {
            return new List<PersonalBest>();
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"personal_bests?user_id=eq.{_auth.UserId}&select=*");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var rows = await response.Content.ReadFromJsonAsync<List<SupabasePersonalBestRow>>() ?? new();
        return rows.Select(r => new PersonalBest
        {
            ExerciseName = r.ExerciseName,
            Weight = r.Weight,
            Reps = r.Reps,
            SessionId = r.SessionId,
            AchievedAt = r.AchievedAt,
        }).ToList();
    }

    public async Task DeletePersonalBestAsync(string exerciseName)
    {
        var token = await _auth.GetValidAccessTokenAsync()
            ?? throw new InvalidOperationException("Not signed in.");

        var request = new HttpRequestMessage(HttpMethod.Delete, $"personal_bests?exercise_name=eq.{Uri.EscapeDataString(exerciseName)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task PushBodyWeightAsync(BodyWeightEntry entry)
    {
        var token = await _auth.GetValidAccessTokenAsync()
            ?? throw new InvalidOperationException("Not signed in.");

        var payload = new
        {
            user_id = _auth.UserId,
            date = entry.Date.ToString("yyyy-MM-dd"),
            weight_kg = entry.WeightKg,
            is_clothed = entry.IsClothed,
            updated_at = entry.UpdatedAt,
            deleted_at = (DateTime?)null,
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "body_weight_entries")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Prefer", "resolution=merge-duplicates,return=minimal");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SyncResult<BodyWeightEntry, DateOnly>> PullAllBodyWeightsAsync()
    {
        var token = await _auth.GetValidAccessTokenAsync();
        if (token is null)
        {
            return new SyncResult<BodyWeightEntry, DateOnly>();
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"body_weight_entries?user_id=eq.{_auth.UserId}&select=*");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var rows = await response.Content.ReadFromJsonAsync<List<SupabaseBodyWeightRow>>() ?? new();
        return new SyncResult<BodyWeightEntry, DateOnly>
        {
            AllKnownKeys = rows.Select(r => DateOnly.Parse(r.Date)).ToHashSet(),
            Active = rows
                .Where(r => r.DeletedAt is null)
                .Select(r => new BodyWeightEntry
                {
                    Date = DateOnly.Parse(r.Date),
                    WeightKg = r.WeightKg,
                    IsClothed = r.IsClothed,
                    UpdatedAt = r.UpdatedAt,
                })
                .ToList(),
        };
    }

    public async Task DeleteBodyWeightAsync(DateOnly date)
    {
        var token = await _auth.GetValidAccessTokenAsync()
            ?? throw new InvalidOperationException("Not signed in.");

        // Soft delete - see the comment in DeleteSessionAsync for why a hard DELETE is unsafe here.
        var request = new HttpRequestMessage(HttpMethod.Patch, $"body_weight_entries?date=eq.{date:yyyy-MM-dd}")
        {
            Content = JsonContent.Create(new { deleted_at = DateTime.UtcNow }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Prefer", "return=minimal");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private class SupabaseBodyWeightRow
    {
        [JsonPropertyName("date")] public string Date { get; set; } = "";
        [JsonPropertyName("weight_kg")] public decimal WeightKg { get; set; }
        [JsonPropertyName("is_clothed")] public bool IsClothed { get; set; }
        [JsonPropertyName("updated_at")] public DateTime UpdatedAt { get; set; }
        [JsonPropertyName("deleted_at")] public DateTime? DeletedAt { get; set; }
    }

    private class SupabaseSessionRow
    {
        [JsonPropertyName("id")] public Guid Id { get; set; }
        [JsonPropertyName("date")] public string Date { get; set; } = "";
        [JsonPropertyName("day_name")] public string DayName { get; set; } = "";
        [JsonPropertyName("data")] public List<ExerciseLog> Data { get; set; } = new();
        [JsonPropertyName("updated_at")] public DateTime UpdatedAt { get; set; }
        [JsonPropertyName("notes")] public string? Notes { get; set; }
        [JsonPropertyName("deleted_at")] public DateTime? DeletedAt { get; set; }
    }

    private class SupabasePersonalBestRow
    {
        [JsonPropertyName("exercise_name")] public string ExerciseName { get; set; } = "";
        [JsonPropertyName("weight")] public decimal Weight { get; set; }
        [JsonPropertyName("reps")] public int Reps { get; set; }
        [JsonPropertyName("session_id")] public Guid SessionId { get; set; }
        [JsonPropertyName("achieved_at")] public DateTime AchievedAt { get; set; }
    }
}
