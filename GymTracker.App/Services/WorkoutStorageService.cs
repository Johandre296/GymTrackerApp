using Blazored.LocalStorage;
using GymTracker.App.Models;

namespace GymTracker.App.Services;

public class WorkoutStorageService : IWorkoutStorageService
{
    private const string StorageKey = "gym-tracker-sessions";
    private readonly ILocalStorageService _localStorage;
    private readonly ICloudSyncService _cloudSync;

    public WorkoutStorageService(ILocalStorageService localStorage, ICloudSyncService cloudSync)
    {
        _localStorage = localStorage;
        _cloudSync = cloudSync;
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
        session.UpdatedAt = DateTime.UtcNow;

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

        try
        {
            await _cloudSync.PushSessionAsync(session);
        }
        catch
        {
            // Offline or sync failure - local save already succeeded; SyncWithCloudAsync will catch up later.
        }
    }

    public async Task SyncWithCloudAsync()
    {
        Models.SyncResult<WorkoutSession, Guid> result;
        try
        {
            result = await _cloudSync.PullAllAsync();
        }
        catch
        {
            return;
        }

        var localSessions = await GetAllSessionsAsync();

        // The server knows about this id (active or soft-deleted) but no longer has it active -
        // it was deleted elsewhere, so drop the stale local copy instead of keeping or re-pushing it.
        localSessions.RemoveAll(s => result.AllKnownKeys.Contains(s.Id) && result.Active.All(r => r.Id != s.Id));

        var merged = localSessions.ToDictionary(s => s.Id);
        foreach (var remote in result.Active)
        {
            if (!merged.TryGetValue(remote.Id, out var local) || remote.UpdatedAt > local.UpdatedAt)
            {
                merged[remote.Id] = remote;
            }
        }

        await _localStorage.SetItemAsync(StorageKey, merged.Values.ToList());

        // Only push sessions the server has never seen at all - anything it already knows about
        // (even soft-deleted) must not be blindly re-pushed, or a deletion could be undone by a
        // device that was offline when it happened.
        foreach (var local in localSessions.Where(s => !result.AllKnownKeys.Contains(s.Id)))
        {
            try
            {
                await _cloudSync.PushSessionAsync(local);
            }
            catch
            {
                // still offline or this one failed - next sync will retry
            }
        }
    }

    public async Task DeleteSessionAsync(Guid sessionId)
    {
        var sessions = await GetAllSessionsAsync();
        sessions.RemoveAll(s => s.Id == sessionId);
        await _localStorage.SetItemAsync(StorageKey, sessions);

        try
        {
            await _cloudSync.DeleteSessionAsync(sessionId);
        }
        catch
        {
            // Offline - the session isn't gone server-side yet, so it could reappear on the next
            // sync. Acceptable for a personal app; deleting again once back online clears it for good.
        }
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
