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
            body_weight_kg = session.BodyWeightKg,
            notes = session.Notes,
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

    public async Task<List<WorkoutSession>> PullAllAsync()
    {
        var token = await _auth.GetValidAccessTokenAsync();
        if (token is null)
        {
            return new List<WorkoutSession>();
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"workout_sessions?user_id=eq.{_auth.UserId}&select=*");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var rows = await response.Content.ReadFromJsonAsync<List<SupabaseSessionRow>>() ?? new();
        return rows.Select(r => new WorkoutSession
        {
            Id = r.Id,
            Date = DateOnly.Parse(r.Date),
            DayName = r.DayName,
            Exercises = r.Data,
            UpdatedAt = r.UpdatedAt,
            BodyWeightKg = r.BodyWeightKg,
            Notes = r.Notes ?? string.Empty,
        }).ToList();
    }

    public async Task DeleteSessionAsync(Guid sessionId)
    {
        var token = await _auth.GetValidAccessTokenAsync()
            ?? throw new InvalidOperationException("Not signed in.");

        var request = new HttpRequestMessage(HttpMethod.Delete, $"workout_sessions?id=eq.{sessionId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

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

    private class SupabaseSessionRow
    {
        [JsonPropertyName("id")] public Guid Id { get; set; }
        [JsonPropertyName("date")] public string Date { get; set; } = "";
        [JsonPropertyName("day_name")] public string DayName { get; set; } = "";
        [JsonPropertyName("data")] public List<ExerciseLog> Data { get; set; } = new();
        [JsonPropertyName("updated_at")] public DateTime UpdatedAt { get; set; }
        [JsonPropertyName("body_weight_kg")] public decimal? BodyWeightKg { get; set; }
        [JsonPropertyName("notes")] public string? Notes { get; set; }
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
