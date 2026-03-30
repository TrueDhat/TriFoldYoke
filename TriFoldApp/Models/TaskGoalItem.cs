using SQLite;

namespace TriFoldApp.Models;

[Table("task_goals")]
public class TaskGoalItem
{
	[PrimaryKey]
	public TaskCategory Category { get; set; }
	public int TargetMinutes { get; set; }
}
