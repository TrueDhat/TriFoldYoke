using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using TriFoldApp.Models;
using TriFoldApp.Services;

namespace TriFoldApp.ViewModels;

public class TodayViewModel : BaseViewModel
{
	private readonly ITrackerRepository _repository;
	private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
	private const string TriFoldAssetName = "tatt_work_alt.svg";
	private HtmlWebViewSource _triFoldImageSource = new() { Html = "<html><body>Loading...</body></html>" };
	private static readonly TaskCategory[] TriFoldTaskCategories = [TaskCategory.Mind, TaskCategory.Spirit];
	private int _mindGoalTargetMinutes = 30;
	private int _spiritGoalTargetMinutes = 30;
	private int _mindCompletedMinutesToday;
	private int _spiritCompletedMinutesToday;

	public TodayViewModel(ITrackerRepository repository)
	{
		_repository = repository;
		DeleteTaskCommand = new Command<TrackerTaskItem>(task => _ = DeleteTaskAsync(task));
		RefreshCommand = new Command(() => _ = LoadAsync());
		_ = LoadAsync();
	}

	public ObservableCollection<TrackerTaskItem> TodayTasks { get; } = [];
	public ObservableCollection<WorkoutEntry> RecentWorkouts { get; } = [];
	public ObservableCollection<TaskTemplateItem> MindSubtaskGoals { get; } = [];
	public ObservableCollection<TaskTemplateItem> SpiritSubtaskGoals { get; } = [];
	public HtmlWebViewSource TriFoldImageSource
	{
		get => _triFoldImageSource;
		private set
		{
			_triFoldImageSource = value;
			OnPropertyChanged();
		}
	}

	public string TodayLabel => DateTime.Today.ToString("dddd, MMM d");

	public int OpenTasksCount => TodayTasks.Count(t => !t.IsCompleted);
	public int CompletedTasksCount => TodayTasks.Count(t => t.IsCompleted);
	public int WeeklyWorkoutMinutes => RecentWorkouts.Where(w => w.CompletedOn >= DateTime.Today.AddDays(-7)).Sum(w => w.DurationMinutes);
	public int TodayCompletionRate => TodayTasks.Count == 0 ? 0 : (int)Math.Round((double)CompletedTasksCount / TodayTasks.Count * 100d);
	public int DailyCompletionPercent { get; private set; }
	public int WorkoutProgressPercent { get; private set; }
	public int MindGoalPercent => MindGoalTargetMinutes <= 0
		? 0
		: (int)Math.Round(Math.Min(1d, MindCompletedMinutesToday / (double)MindGoalTargetMinutes) * 100d);
	public int SpiritGoalPercent => SpiritGoalTargetMinutes <= 0
		? 0
		: (int)Math.Round(Math.Min(1d, SpiritCompletedMinutesToday / (double)SpiritGoalTargetMinutes) * 100d);
	public string TriFoldStatusLabel => $"Tri-Fold fill: {Math.Round((MindGoalPercent + SpiritGoalPercent) / 2.0)}%";
	public string DailyProgressLabel => MindGoalTargetMinutes > 0
		? $"Mind progress: {MindCompletedMinutesToday}/{MindGoalTargetMinutes} min ({MindGoalPercent}%)"
		: $"Mind progress: {MindCompletedMinutesToday} min logged (set subtask goals)";
	public string WorkoutProgressLabel => SpiritGoalTargetMinutes > 0
		? $"Spirit progress: {SpiritCompletedMinutesToday}/{SpiritGoalTargetMinutes} min ({SpiritGoalPercent}%)"
		: $"Spirit progress: {SpiritCompletedMinutesToday} min logged (set subtask goals)";
	public string MindSubtaskGoalsHeader => MindGoalTargetMinutes > 0
		? $"Mind Subtask Goals ({MindCompletedMinutesToday}/{MindGoalTargetMinutes} min, {MindGoalPercent}%)"
		: $"Mind Subtask Goals ({MindCompletedMinutesToday} min logged)";
	public string SpiritSubtaskGoalsHeader => SpiritGoalTargetMinutes > 0
		? $"Spirit Subtask Goals ({SpiritCompletedMinutesToday}/{SpiritGoalTargetMinutes} min, {SpiritGoalPercent}%)"
		: $"Spirit Subtask Goals ({SpiritCompletedMinutesToday} min logged)";
	public string MindGoalProgressLabel => MindGoalTargetMinutes > 0
		? $"Mind: {MindCompletedMinutesToday}/{MindGoalTargetMinutes} min ({MindGoalPercent}%)"
		: $"Mind: {MindCompletedMinutesToday} min logged (set subtask goals)";
	public string SpiritGoalProgressLabel => SpiritGoalTargetMinutes > 0
		? $"Spirit: {SpiritCompletedMinutesToday}/{SpiritGoalTargetMinutes} min ({SpiritGoalPercent}%)"
		: $"Spirit: {SpiritCompletedMinutesToday} min logged (set subtask goals)";

	public int MindGoalTargetMinutes
	{
		get => _mindGoalTargetMinutes;
		private set
		{
			if (_mindGoalTargetMinutes == value)
			{
				return;
			}

			_mindGoalTargetMinutes = value;
			OnPropertyChanged();
		}
	}

	public int SpiritGoalTargetMinutes
	{
		get => _spiritGoalTargetMinutes;
		private set
		{
			if (_spiritGoalTargetMinutes == value)
			{
				return;
			}

			_spiritGoalTargetMinutes = value;
			OnPropertyChanged();
		}
	}

	public int MindCompletedMinutesToday
	{
		get => _mindCompletedMinutesToday;
		private set
		{
			if (_mindCompletedMinutesToday == value)
			{
				return;
			}

			_mindCompletedMinutesToday = value;
			OnPropertyChanged();
		}
	}

	public int SpiritCompletedMinutesToday
	{
		get => _spiritCompletedMinutesToday;
		private set
		{
			if (_spiritCompletedMinutesToday == value)
			{
				return;
			}

			_spiritCompletedMinutesToday = value;
			OnPropertyChanged();
		}
	}

	public Command<TrackerTaskItem> DeleteTaskCommand { get; }
	public Command RefreshCommand { get; }

	public Task RefreshAsync() => LoadAsync();

	private async Task LoadAsync()
	{
		await _loadSemaphore.WaitAsync();
		try
		{
			TodayTasks.Clear();
			var tasks = await _repository.GetTasksAsync();
			foreach (var task in tasks.Where(t =>
				t.DueDate.Date <= DateTime.Today.Date &&
				TriFoldTaskCategories.Contains(t.Category)))
			{
				TodayTasks.Add(task);
			}

			RecentWorkouts.Clear();
			var workouts = await _repository.GetWorkoutsAsync();
			foreach (var workout in workouts.OrderByDescending(w => w.CompletedOn).Take(5))
			{
				RecentWorkouts.Add(workout);
			}

			var todayDailyTasks = tasks
				.Where(t => TriFoldTaskCategories.Contains(t.Category) && t.DueDate.Date <= DateTime.Today)
				.ToList();
			var completedTemplateMinutesById = todayDailyTasks
				.Where(t => t.IsCompleted && !string.IsNullOrWhiteSpace(t.TaskTemplateId))
				.GroupBy(t => t.TaskTemplateId!)
				.ToDictionary(g => g.Key, g => g.Sum(t => t.DurationMinutes ?? 0));

			MindCompletedMinutesToday = todayDailyTasks
				.Where(t => t.Category == TaskCategory.Mind && t.IsCompleted)
				.Sum(t => t.DurationMinutes ?? 0);
			SpiritCompletedMinutesToday = todayDailyTasks
				.Where(t => t.Category == TaskCategory.Spirit && t.IsCompleted)
				.Sum(t => t.DurationMinutes ?? 0);
			MindGoalTargetMinutes = await LoadSubtaskGoalProgressAsync(TaskCategory.Mind, MindSubtaskGoals, completedTemplateMinutesById);
			SpiritGoalTargetMinutes = await LoadSubtaskGoalProgressAsync(TaskCategory.Spirit, SpiritSubtaskGoals, completedTemplateMinutesById);

			DailyCompletionPercent = todayDailyTasks.Count == 0
				? 0
				: (int)Math.Round(todayDailyTasks.Count(t => t.IsCompleted) * 100d / todayDailyTasks.Count);

			var workoutsLastWeek = workouts.Count(w => w.CompletedOn.Date >= DateTime.Today.AddDays(-6) && w.CompletedOn.Date <= DateTime.Today);
			var weeklyGoal = 4d;
			WorkoutProgressPercent = (int)Math.Round(Math.Min(1d, workoutsLastWeek / weeklyGoal) * 100d);

			await UpdateTriFoldImageAsync(MindGoalPercent / 100d, SpiritGoalPercent / 100d);

			OnPropertyChanged(nameof(OpenTasksCount));
			OnPropertyChanged(nameof(CompletedTasksCount));
			OnPropertyChanged(nameof(WeeklyWorkoutMinutes));
			OnPropertyChanged(nameof(TodayCompletionRate));
			OnPropertyChanged(nameof(DailyCompletionPercent));
			OnPropertyChanged(nameof(WorkoutProgressPercent));
			OnPropertyChanged(nameof(TriFoldStatusLabel));
			OnPropertyChanged(nameof(DailyProgressLabel));
			OnPropertyChanged(nameof(WorkoutProgressLabel));
			OnPropertyChanged(nameof(MindGoalProgressLabel));
			OnPropertyChanged(nameof(SpiritGoalProgressLabel));
			OnPropertyChanged(nameof(MindSubtaskGoalsHeader));
			OnPropertyChanged(nameof(SpiritSubtaskGoalsHeader));
			OnPropertyChanged(nameof(MindGoalPercent));
			OnPropertyChanged(nameof(SpiritGoalPercent));
		}
		finally
		{
			_loadSemaphore.Release();
		}
	}

	private async Task<int> LoadSubtaskGoalProgressAsync(
		TaskCategory category,
		ObservableCollection<TaskTemplateItem> targetCollection,
		IReadOnlyDictionary<string, int> completedTemplateMinutesById)
	{
		targetCollection.Clear();
		var templates = await _repository.GetTaskTemplatesAsync(category);
		var goalTemplates = templates.Where(t => t.GoalMinutes is > 0).OrderBy(t => t.Title).ToList();
		foreach (var template in goalTemplates)
		{
			template.CompletedMinutesToday = completedTemplateMinutesById.TryGetValue(template.Id, out var completedMinutes)
				? completedMinutes
				: 0;
			targetCollection.Add(template);
		}

		return goalTemplates.Sum(t => t.GoalMinutes ?? 0);
	}

	private async Task UpdateTriFoldImageAsync(double mindProgress, double spiritProgress)
	{
		var svgTemplate = await GetTriFoldSvgTemplateAsync();
		var combinedProgress = Math.Clamp((mindProgress + spiritProgress) / 2d, 0d, 1d);
		var firstFill = Math.Clamp(combinedProgress * 3d, 0d, 1d);
		var secondFill = Math.Clamp(mindProgress, 0d, 1d);
		var thirdFill = Math.Clamp(spiritProgress, 0d, 1d);

		var svg = svgTemplate;
		svg = SetPathFill(svg, "path6", "#5B8DEF", firstFill);
		svg = SetPathFill(svg, "path1", "#35B89A", secondFill);
		svg = SetPathFill(svg, "path1-8", "#F59E0B", thirdFill);

		var html =
			"<html><head>" +
			"<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />" +
			"<style>" +
			"html, body { margin: 0; padding: 0; background: transparent; overflow: hidden; }" +
			"svg { width: 100%; height: auto; max-height: 310px; display: block; margin: 0 auto; }" +
			"</style>" +
			"</head><body>" +
			svg +
			"</body></html>";

		TriFoldImageSource = new HtmlWebViewSource { Html = html };
	}

	private async Task<string> GetTriFoldSvgTemplateAsync()
	{
		using var stream = await FileSystem.OpenAppPackageFileAsync(TriFoldAssetName);
		using var reader = new StreamReader(stream);
		return await reader.ReadToEndAsync();
	}

	private static string SetPathFill(string svg, string pathId, string fillColor, double fillOpacity)
	{
		var pattern = $"""(<path\b[^>]*\bid="{Regex.Escape(pathId)}"[^>]*\bstyle=")([^"]*)(")""";
		var regex = new Regex(pattern);
		return regex.Replace(svg, match =>
		{
			var style = match.Groups[2].Value;
			var updatedStyle = UpsertStyleValue(style, "fill", fillColor);
			updatedStyle = UpsertStyleValue(updatedStyle, "fill-opacity", fillOpacity.ToString("0.###", CultureInfo.InvariantCulture));
			return $"{match.Groups[1].Value}{updatedStyle}{match.Groups[3].Value}";
		}, 1);
	}

	private static string UpsertStyleValue(string style, string key, string value)
	{
		var entries = style
			.Split(';', StringSplitOptions.RemoveEmptyEntries)
			.Select(s => s.Split(':', 2))
			.Where(parts => parts.Length == 2)
			.ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);

		entries[key] = value;
		return string.Join(";", entries.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
	}

	private async Task DeleteTaskAsync(TrackerTaskItem? task)
	{
		if (task is null)
		{
			return;
		}

		await _repository.DeleteTaskAsync(task.Id);
		await LoadAsync();
	}
}
