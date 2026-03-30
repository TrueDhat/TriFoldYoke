using SQLite;

namespace TriFoldApp.Models;

[Table("workout_plans")]
public class WorkoutPlanItem
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	public string TemplateId { get; set; } = string.Empty;
	public string TemplateName { get; set; } = string.Empty;
	public DateTime PlannedDate { get; set; } = DateTime.Today;
	public bool IsCompleted { get; set; }
	public string? CompletedWorkoutId { get; set; }
}
