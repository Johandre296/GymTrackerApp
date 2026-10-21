using GymTracker.App.Models;

namespace GymTracker.App.Services;

public interface IPersonalBestService
{
    Task<PersonalBest?> GetAsync(string exerciseName);

    /// <summary>Records the set as the new PB if it beats the current one. Returns the new PB, or null if it wasn't an improvement.</summary>
    Task<PersonalBest?> CheckAndUpdateAsync(string exerciseName, decimal weight, int reps, Guid sessionId);

    /// <summary>Recomputes the PB for one exercise from scratch by scanning all sessions - used after a set or session is deleted.</summary>
    Task RecomputeAsync(string exerciseName, IEnumerable<WorkoutSession> allSessions);

    Task SyncWithCloudAsync();
}
