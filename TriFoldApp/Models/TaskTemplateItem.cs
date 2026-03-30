using SQLite;

namespace TriFoldApp.Models;

[Table("task_templates")]
public class TaskTemplateItem
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	public string Title { get; set; } = string.Empty;
	public TaskCategory Category { get; set; }
	public int? DurationMinutes { get; set; }
	public int? GoalMinutes { get; set; }

	[Ignore]
	public string GoalInputMinutes { get; set; } = string.Empty;

	[Ignore]
	public int CompletedMinutesToday { get; set; }

	[Ignore]
	public int SubgoalPercent => GoalMinutes is > 0
		? (int)Math.Round(Math.Min(1d, CompletedMinutesToday / (double)GoalMinutes.Value) * 100d)
		: 0;

	[Ignore]
	public string SubgoalProgressLabel => GoalMinutes is > 0
		? $"{CompletedMinutesToday}/{GoalMinutes} min ({SubgoalPercent}%)"
		: "No subgoal set";
}
