namespace TriFoldApp.Models;

public class TaskSelectionOption
{
	public string? TemplateId { get; init; }
	public string Title { get; init; } = string.Empty;
	public bool IsOther { get; init; }
	public string DisplayLabel => IsOther ? "Other Task" : Title;
}
