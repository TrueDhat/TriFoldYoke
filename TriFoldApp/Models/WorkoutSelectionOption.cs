namespace TriFoldApp.Models;

public class WorkoutSelectionOption
{
	public string? TemplateId { get; init; }
	public string Name { get; init; } = string.Empty;
	public bool IsOther { get; init; }
	public string DisplayLabel => IsOther ? "Other Workout" : Name;
}
