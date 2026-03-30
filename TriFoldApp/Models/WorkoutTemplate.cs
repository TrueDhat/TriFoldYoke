using SQLite;

namespace TriFoldApp.Models;

[Table("workout_templates")]
public class WorkoutTemplate
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	public string Name { get; set; } = string.Empty;
	public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
