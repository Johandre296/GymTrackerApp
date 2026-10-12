namespace GymTracker.App.Models;

public class ExerciseLog
{
    public string ExerciseName { get; set; } = string.Empty;
    public List<SetEntry> Sets { get; set; } = new();
}
