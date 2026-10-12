namespace GymTracker.App.Models;

public class DayProgram
{
    public string DayName { get; set; } = string.Empty;
    public string FocusLabel { get; set; } = string.Empty;
    public ExerciseDefinition MainLift { get; set; } = new();
    public ExerciseDefinition AssistLift { get; set; } = new();
}
