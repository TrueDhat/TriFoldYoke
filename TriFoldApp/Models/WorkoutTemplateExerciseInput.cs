namespace TriFoldApp.Models;

public class WorkoutTemplateExerciseInput
{
	public string Name { get; set; } = string.Empty;
	public int Sets { get; set; }
	public int Reps { get; set; }
	public double Weight { get; set; }
	public string WeightUnit { get; set; } = "lb";
}
