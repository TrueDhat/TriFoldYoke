namespace TriFoldApp.Models;

public class WorkoutTemplateExerciseDraft
{
	public string Name { get; set; } = string.Empty;
	public string Sets { get; set; } = string.Empty;
	public string Reps { get; set; } = string.Empty;
	public string Weight { get; set; } = string.Empty;
	public string WeightUnit { get; set; } = "lb";
}
