using GymTracker.App.Models;

namespace GymTracker.App.Services;

public interface ICloudSyncService
{
    Task PushSessionAsync(WorkoutSession session);
    Task<List<WorkoutSession>> PullAllAsync();
}
