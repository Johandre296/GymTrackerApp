using GymTracker.App.Models;

namespace GymTracker.App.Services;

public interface ICloudSyncService
{
    Task PushSessionAsync(WorkoutSession session);
    Task<SyncResult<WorkoutSession, Guid>> PullAllAsync();
    Task DeleteSessionAsync(Guid sessionId);

    Task PushPersonalBestAsync(PersonalBest personalBest);
    Task<List<PersonalBest>> PullAllPersonalBestsAsync();
    Task DeletePersonalBestAsync(string exerciseName);

    Task PushBodyWeightAsync(BodyWeightEntry entry);
    Task<SyncResult<BodyWeightEntry, DateOnly>> PullAllBodyWeightsAsync();
    Task DeleteBodyWeightAsync(DateOnly date);
}
