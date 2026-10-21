namespace GymTracker.App.Models;

public class BodyWeightEntry
{
    public DateOnly Date { get; set; }
    public decimal WeightKg { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
