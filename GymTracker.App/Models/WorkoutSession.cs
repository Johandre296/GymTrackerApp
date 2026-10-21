namespace GymTracker.App.Models;

public class WorkoutSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public List<ExerciseLog> Exercises { get; set; } = new();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public decimal? BodyWeightKg { get; set; }
    public string Notes { get; set; } = string.Empty;
}
