namespace TriFoldApp.Models;

public class WorkoutExerciseLogInput
{
	public string TemplateExerciseId { get; set; } = string.Empty;
	public string ExerciseName { get; set; } = string.Empty;
	public int Sets { get; set; }
	public int Reps { get; set; }
	public double Weight { get; set; }
	public string WeightUnit { get; set; } = "lb";
}
