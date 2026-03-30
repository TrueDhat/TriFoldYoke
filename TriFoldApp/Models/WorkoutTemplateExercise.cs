using SQLite;

namespace TriFoldApp.Models;

[Table("workout_template_exercises")]
public class WorkoutTemplateExercise
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	[Indexed]
	public string TemplateId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public int Sets { get; set; }
	public int Reps { get; set; }
	public double Weight { get; set; }
	public string WeightUnit { get; set; } = "lb";
}
