using SQLite;

namespace TriFoldApp.Models;

[Table("workout_session_exercises")]
public class WorkoutSessionExercise
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	[Indexed]
	public string WorkoutId { get; set; } = string.Empty;
	[Indexed]
	public string TemplateId { get; set; } = string.Empty;
	public string TemplateExerciseId { get; set; } = string.Empty;
	public string ExerciseName { get; set; } = string.Empty;
	public int Sets { get; set; }
	public int Reps { get; set; }
	public double Weight { get; set; }
	public string WeightUnit { get; set; } = "lb";
	public DateTime CompletedOn { get; set; } = DateTime.Today;
}
