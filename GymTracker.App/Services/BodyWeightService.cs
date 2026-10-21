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

    public async Task SetAsync(DateOnly date, decimal weightKg)
    {
        var all = await GetAllAsync();
        var entry = all.FirstOrDefault(e => e.Date == date);
        if (entry is null)
        {
            entry = new BodyWeightEntry { Date = date };
            all.Add(entry);
        }

        entry.WeightKg = weightKg;
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
        List<BodyWeightEntry> remote;
        try
        {
            remote = await _cloudSync.PullAllBodyWeightsAsync();
        }
        catch
        {
            return;
        }

        var local = await GetAllAsync();
        var merged = local.ToDictionary(e => e.Date);

        foreach (var r in remote)
        {
            if (!merged.TryGetValue(r.Date, out var existing) || r.UpdatedAt > existing.UpdatedAt)
            {
                merged[r.Date] = r;
            }
        }

        await _localStorage.SetItemAsync(StorageKey, merged.Values.ToList());

        var remoteDates = remote.Select(r => r.Date).ToHashSet();
        foreach (var localOnly in local.Where(e => !remoteDates.Contains(e.Date)))
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
