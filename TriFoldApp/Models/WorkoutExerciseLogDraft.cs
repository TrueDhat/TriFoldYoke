namespace TriFoldApp.Models;

public class WorkoutExerciseLogDraft
{
	public string SessionExerciseId { get; set; } = string.Empty;
	public string WorkoutId { get; set; } = string.Empty;
	public string TemplateExerciseId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Sets { get; set; } = string.Empty;
	public string Reps { get; set; } = string.Empty;
	public string Weight { get; set; } = string.Empty;
	public string WeightUnit { get; set; } = "lb";
	public string SetColorHex { get; set; } = "#8F00FF";
}
