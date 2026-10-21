using Blazored.LocalStorage;
using GymTracker.App.Models;

namespace GymTracker.App.Services;

public class PersonalBestService : IPersonalBestService
{
    private const string StorageKey = "gym-tracker-personal-bests";
    private readonly ILocalStorageService _localStorage;
    private readonly ICloudSyncService _cloudSync;

    public PersonalBestService(ILocalStorageService localStorage, ICloudSyncService cloudSync)
    {
        _localStorage = localStorage;
        _cloudSync = cloudSync;
    }

    public async Task<PersonalBest?> GetAsync(string exerciseName)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(p => p.ExerciseName == exerciseName);
    }

    public async Task<PersonalBest?> CheckAndUpdateAsync(string exerciseName, decimal weight, int reps, Guid sessionId)
    {
        var all = await GetAllAsync();
        var existing = all.FirstOrDefault(p => p.ExerciseName == exerciseName);
        if (existing is not null && weight <= existing.Weight)
        {
            return null;
        }

        var personalBest = new PersonalBest
        {
            ExerciseName = exerciseName,
            Weight = weight,
            Reps = reps,
            SessionId = sessionId,
            AchievedAt = DateTime.UtcNow,
        };

        all.RemoveAll(p => p.ExerciseName == exerciseName);
        all.Add(personalBest);
        await _localStorage.SetItemAsync(StorageKey, all);

        try
        {
            await _cloudSync.PushPersonalBestAsync(personalBest);
        }
        catch
        {
            // Offline - local record already updated; SyncWithCloudAsync will catch up later.
        }

        return personalBest;
    }

    public async Task RecomputeAsync(string exerciseName, IEnumerable<WorkoutSession> allSessions)
    {
        var sessions = allSessions.ToList();
        WorkoutSession? bestSession = null;
        SetEntry? bestSet = null;

        foreach (var session in sessions)
        {
            var log = session.Exercises.FirstOrDefault(e => e.ExerciseName == exerciseName);
            if (log is null)
            {
                continue;
            }

            foreach (var set in log.Sets.Where(s => !s.IsWarmup))
            {
                if (bestSet is null || set.Weight > bestSet.Weight)
                {
                    bestSet = set;
                    bestSession = session;
                }
            }
        }

        var all = await GetAllAsync();
        all.RemoveAll(p => p.ExerciseName == exerciseName);

        if (bestSet is not null && bestSession is not null)
        {
            var personalBest = new PersonalBest
            {
                ExerciseName = exerciseName,
                Weight = bestSet.Weight,
                Reps = bestSet.Reps,
                SessionId = bestSession.Id,
                AchievedAt = DateTime.UtcNow,
            };
            all.Add(personalBest);
            await _localStorage.SetItemAsync(StorageKey, all);

            try
            {
                await _cloudSync.PushPersonalBestAsync(personalBest);
            }
            catch
            {
                // Offline - will catch up on next sync.
            }
        }
        else
        {
            await _localStorage.SetItemAsync(StorageKey, all);

            try
            {
                await _cloudSync.DeletePersonalBestAsync(exerciseName);
            }
            catch
            {
                // Offline - will catch up on next sync (the stale cloud row just lingers until then).
            }
        }
    }

    public async Task SyncWithCloudAsync()
    {
        List<PersonalBest> remote;
        try
        {
            remote = await _cloudSync.PullAllPersonalBestsAsync();
        }
        catch
        {
            return;
        }

        var local = await GetAllAsync();
        var merged = local.ToDictionary(p => p.ExerciseName);

        foreach (var r in remote)
        {
            if (!merged.TryGetValue(r.ExerciseName, out var existing) || r.AchievedAt > existing.AchievedAt)
            {
                merged[r.ExerciseName] = r;
            }
        }

        await _localStorage.SetItemAsync(StorageKey, merged.Values.ToList());

        var remoteNames = remote.Select(r => r.ExerciseName).ToHashSet();
        foreach (var localOnly in local.Where(p => !remoteNames.Contains(p.ExerciseName)))
        {
            try
            {
                await _cloudSync.PushPersonalBestAsync(localOnly);
            }
            catch
            {
                // still offline or this one failed - next sync will retry
            }
        }
    }

    private async Task<List<PersonalBest>> GetAllAsync()
    {
        var list = await _localStorage.GetItemAsync<List<PersonalBest>>(StorageKey);
        return list ?? new List<PersonalBest>();
    }
}
