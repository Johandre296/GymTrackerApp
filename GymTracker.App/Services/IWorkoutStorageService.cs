using GymTracker.App.Models;

namespace GymTracker.App.Services;

public interface IWorkoutStorageService
{
    Task<List<WorkoutSession>> GetAllSessionsAsync();
    Task<WorkoutSession> GetOrCreateSessionAsync(DateOnly date, string dayName);
    Task SaveSessionAsync(WorkoutSession session);
    Task DeleteSessionAsync(Guid sessionId);
    Task<List<SetEntry>> GetMostRecentSetsAsync(string exerciseName, Guid excludeSessionId);
}
