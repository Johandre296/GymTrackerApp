namespace GymTracker.App.Models;

public class SetEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal Weight { get; set; }
    public int Reps { get; set; }
    public bool IsWarmup { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
