using GymTracker.App.Models;

namespace GymTracker.App.Data;

public static class WorkoutProgram
{
    public static readonly List<DayProgram> Days = new()
    {
        new DayProgram
        {
            DayName = "Monday",
            FocusLabel = "Heavy Squat Day",
            MainLift = new ExerciseDefinition { Name = "Squat" },
            AssistLift = new ExerciseDefinition { Name = "Leg Extension" },
        },
        new DayProgram
        {
            DayName = "Wednesday",
            FocusLabel = "Heavy Bench Day",
            MainLift = new ExerciseDefinition { Name = "Bench Press" },
            AssistLift = new ExerciseDefinition { Name = "Dips", AllowsNegativeWeight = true },
        },
        new DayProgram
        {
            DayName = "Friday",
            FocusLabel = "Heavy Deadlift Day",
            MainLift = new ExerciseDefinition { Name = "Deadlift" },
            AssistLift = new ExerciseDefinition { Name = "Pull-ups", AllowsNegativeWeight = true },
        },
    };

    public static DayProgram? GetByName(string dayName) =>
        Days.FirstOrDefault(d => d.DayName == dayName);

    public static DayProgram GetForToday()
    {
        var today = DateTime.Now.DayOfWeek.ToString();
        return Days.FirstOrDefault(d => d.DayName == today) ?? Days[0];
    }
}
