namespace GymTracker.App.Models;

public class PersonalBest
{
    public string ExerciseName { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public int Reps { get; set; }
    public Guid SessionId { get; set; }
    public DateTime AchievedAt { get; set; } = DateTime.UtcNow;
}
