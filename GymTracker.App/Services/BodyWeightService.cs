using Blazored.LocalStorage;
using GymTracker.App.Models;

namespace GymTracker.App.Services;

public class BodyWeightService : IBodyWeightService
{
    private const string StorageKey = "gym-tracker-bodyweight";
    private readonly ILocalStorageService _localStorage;
    private readonly ICloudSyncService _cloudSync;

    public BodyWeightService(ILocalStorageService localStorage, ICloudSyncService cloudSync)
    {
        _localStorage = localStorage;
        _cloudSync = cloudSync;
    }

    public async Task<List<BodyWeightEntry>> GetAllAsync()
    {
        var list = await _localStorage.GetItemAsync<List<BodyWeightEntry>>(StorageKey);
        return list ?? new List<BodyWeightEntry>();
    }

    public async Task<BodyWeightEntry?> GetForDateAsync(DateOnly date)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(e => e.Date == date);
    }

    public async Task SetAsync(DateOnly date, decimal weightKg, bool isClothed)
    {
        var all = await GetAllAsync();
        var entry = all.FirstOrDefault(e => e.Date == date);
        if (entry is null)
        {
            entry = new BodyWeightEntry { Date = date };
            all.Add(entry);
        }

        entry.WeightKg = weightKg;
        entry.IsClothed = isClothed;
        entry.UpdatedAt = DateTime.UtcNow;

        await _localStorage.SetItemAsync(StorageKey, all);

        try
        {
            await _cloudSync.PushBodyWeightAsync(entry);
        }
        catch
        {
            // Offline - local save already succeeded; SyncWithCloudAsync will catch up later.
        }
    }

    public async Task DeleteAsync(DateOnly date)
    {
        var all = await GetAllAsync();
        all.RemoveAll(e => e.Date == date);
        await _localStorage.SetItemAsync(StorageKey, all);

        try
        {
            await _cloudSync.DeleteBodyWeightAsync(date);
        }
        catch
        {
            // Offline - could reappear on the next sync; deleting again once back online clears it for good.
        }
    }

    public async Task SyncWithCloudAsync()
    {
        Models.SyncResult<BodyWeightEntry, DateOnly> result;
        try
        {
            result = await _cloudSync.PullAllBodyWeightsAsync();
        }
        catch
        {
            return;
        }

        var local = await GetAllAsync();

        // Known to the server but no longer active - deleted elsewhere, drop the stale local copy.
        local.RemoveAll(e => result.AllKnownKeys.Contains(e.Date) && result.Active.All(r => r.Date != e.Date));

        var merged = local.ToDictionary(e => e.Date);
        foreach (var r in result.Active)
        {
            if (!merged.TryGetValue(r.Date, out var existing) || r.UpdatedAt > existing.UpdatedAt)
            {
                merged[r.Date] = r;
            }
        }

        await _localStorage.SetItemAsync(StorageKey, merged.Values.ToList());

        // Only push entries the server has never seen at all - see WorkoutStorageService.SyncWithCloudAsync.
        foreach (var localOnly in local.Where(e => !result.AllKnownKeys.Contains(e.Date)))
        {
            try
            {
                await _cloudSync.PushBodyWeightAsync(localOnly);
            }
            catch
            {
                // still offline or this one failed - next sync will retry
            }
        }
    }
}
