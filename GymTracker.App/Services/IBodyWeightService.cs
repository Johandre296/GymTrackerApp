using GymTracker.App.Models;

namespace GymTracker.App.Services;

public interface IBodyWeightService
{
    Task<List<BodyWeightEntry>> GetAllAsync();
    Task<BodyWeightEntry?> GetForDateAsync(DateOnly date);

    /// <summary>Creates or overwrites the entry for this date.</summary>
    Task SetAsync(DateOnly date, decimal weightKg, bool isClothed);

    Task DeleteAsync(DateOnly date);

    Task SyncWithCloudAsync();
}
