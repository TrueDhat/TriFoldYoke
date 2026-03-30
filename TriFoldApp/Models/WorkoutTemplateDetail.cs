namespace TriFoldApp.Models;

public class WorkoutTemplateDetail
{
	public string Id { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public IReadOnlyList<WorkoutTemplateExercise> Exercises { get; init; } = [];
	public int ExerciseCount => Exercises.Count;
	public string ExerciseSummary => string.Join(", ", Exercises.Select(e =>
	{
		var weightPart = e.Weight > 0 ? $" @ {e.Weight:0.##} {e.WeightUnit}" : string.Empty;
		return $"{e.Name} ({e.Sets}x{e.Reps}{weightPart})";
	}));
}
