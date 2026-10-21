using GymTracker.App.Models;

namespace GymTracker.App.Services;

public interface ICloudSyncService
{
    Task PushSessionAsync(WorkoutSession session);
    Task<List<WorkoutSession>> PullAllAsync();
    Task DeleteSessionAsync(Guid sessionId);

    Task PushPersonalBestAsync(PersonalBest personalBest);
    Task<List<PersonalBest>> PullAllPersonalBestsAsync();
    Task DeletePersonalBestAsync(string exerciseName);

    Task PushBodyWeightAsync(BodyWeightEntry entry);
    Task<List<BodyWeightEntry>> PullAllBodyWeightsAsync();
    Task DeleteBodyWeightAsync(DateOnly date);
}
