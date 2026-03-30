using SQLite;

namespace TriFoldApp.Models;

[Table("tasks")]
public class TrackerTaskItem
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	public string Title { get; set; } = string.Empty;
	public TaskCategory Category { get; set; } = TaskCategory.Daily;
	public DateTime DueDate { get; set; } = DateTime.Today;
	public int? DurationMinutes { get; set; }
	public string? TaskTemplateId { get; set; }
	public bool IsCompleted { get; set; }
}
