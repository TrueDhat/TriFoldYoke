using SQLite;

namespace TriFoldApp.Models;

[Table("workouts")]
public class WorkoutEntry
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	public string Name { get; set; } = string.Empty;
	public int DurationMinutes { get; set; }
	public DateTime CompletedOn { get; set; } = DateTime.Today;
}
