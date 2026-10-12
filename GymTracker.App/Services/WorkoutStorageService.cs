using Blazored.LocalStorage;
using GymTracker.App.Models;

namespace GymTracker.App.Services;

public class WorkoutStorageService : IWorkoutStorageService
{
    private const string StorageKey = "gym-tracker-sessions";
    private readonly ILocalStorageService _localStorage;

    public WorkoutStorageService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<List<WorkoutSession>> GetAllSessionsAsync()
    {
        var sessions = await _localStorage.GetItemAsync<List<WorkoutSession>>(StorageKey);
        return sessions ?? new List<WorkoutSession>();
    }

    public async Task<WorkoutSession> GetOrCreateSessionAsync(DateOnly date, string dayName)
    {
        var sessions = await GetAllSessionsAsync();
        var existing = sessions.FirstOrDefault(s => s.Date == date && s.DayName == dayName);
        if (existing is not null)
        {
            return existing;
        }

        return new WorkoutSession { Date = date, DayName = dayName };
    }

    public async Task SaveSessionAsync(WorkoutSession session)
    {
        var sessions = await GetAllSessionsAsync();
        var index = sessions.FindIndex(s => s.Id == session.Id);
        if (index >= 0)
        {
            sessions[index] = session;
        }
        else
        {
            sessions.Add(session);
        }

        await _localStorage.SetItemAsync(StorageKey, sessions);
    }

    public async Task DeleteSessionAsync(Guid sessionId)
    {
        var sessions = await GetAllSessionsAsync();
        sessions.RemoveAll(s => s.Id == sessionId);
        await _localStorage.SetItemAsync(StorageKey, sessions);
    }

    public async Task<List<SetEntry>> GetMostRecentSetsAsync(string exerciseName, Guid excludeSessionId)
    {
        var sessions = await GetAllSessionsAsync();
        var mostRecent = sessions
            .Where(s => s.Id != excludeSessionId)
            .OrderByDescending(s => s.Date)
            .FirstOrDefault(s => s.Exercises.Any(e => e.ExerciseName == exerciseName && e.Sets.Count > 0));

        var log = mostRecent?.Exercises.FirstOrDefault(e => e.ExerciseName == exerciseName);
        return log?.Sets ?? new List<SetEntry>();
    }
}
