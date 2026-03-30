using System.Collections.ObjectModel;
using TriFoldApp.Models;
using TriFoldApp.Services;

namespace TriFoldApp.ViewModels;

public class MindViewModel : BaseViewModel
{
	private readonly ITrackerRepository _repository;
	private bool _isMutating;
	private TaskSelectionOption? _selectedAddTaskOption;
	private string _otherTaskTitle = string.Empty;
	private string _completedTaskDurationMinutes = string.Empty;
	private string _newStandardTaskTitle = string.Empty;
	private DateTime _selectedDueDate = DateTime.Today;

	public MindViewModel(ITrackerRepository repository)
	{
		_repository = repository;
		DeleteTaskCommand = new Command<TrackerTaskItem>(task => _ = DeleteTaskAsync(task));
		AddTaskCommand = new Command(() => _ = AddTaskAsync());
		AddStandardTaskCommand = new Command(() => _ = AddStandardTaskAsync());
		DeleteStandardTaskCommand = new Command<TaskTemplateItem>(template => _ = DeleteStandardTaskAsync(template));
		SaveStandardTaskGoalCommand = new Command<TaskTemplateItem>(template => _ = SaveStandardTaskGoalAsync(template));
		ClearStandardTaskGoalCommand = new Command<TaskTemplateItem>(template => _ = ClearStandardTaskGoalAsync(template));
		RefreshCommand = new Command(() => _ = LoadAsync());
		_ = LoadAsync();
	}

	public ObservableCollection<TrackerTaskItem> Tasks { get; } = [];
	public ObservableCollection<TaskTemplateItem> StandardTasks { get; } = [];
	public ObservableCollection<TaskSelectionOption> AddTaskOptions { get; } = [];
	public Command<TrackerTaskItem> DeleteTaskCommand { get; }
	public Command AddTaskCommand { get; }
	public Command AddStandardTaskCommand { get; }
	public Command<TaskTemplateItem> DeleteStandardTaskCommand { get; }
	public Command<TaskTemplateItem> SaveStandardTaskGoalCommand { get; }
	public Command<TaskTemplateItem> ClearStandardTaskGoalCommand { get; }
	public Command RefreshCommand { get; }

	public TaskSelectionOption? SelectedAddTaskOption
	{
		get => _selectedAddTaskOption;
		set
		{
			if (_selectedAddTaskOption == value)
			{
				return;
			}

			_selectedAddTaskOption = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(IsOtherTaskSelected));
		}
	}

	public bool IsOtherTaskSelected => SelectedAddTaskOption?.IsOther == true;

	public string OtherTaskTitle
	{
		get => _otherTaskTitle;
		set
		{
			if (_otherTaskTitle == value)
			{
				return;
			}

			_otherTaskTitle = value;
			OnPropertyChanged();
		}
	}

	public string CompletedTaskDurationMinutes
	{
		get => _completedTaskDurationMinutes;
		set
		{
			if (_completedTaskDurationMinutes == value)
			{
				return;
			}

			_completedTaskDurationMinutes = value;
			OnPropertyChanged();
		}
	}

	public DateTime SelectedDueDate
	{
		get => _selectedDueDate;
		set
		{
			if (_selectedDueDate == value)
			{
				return;
			}

			_selectedDueDate = value;
			OnPropertyChanged();
		}
	}

	public string NewStandardTaskTitle
	{
		get => _newStandardTaskTitle;
		set
		{
			if (_newStandardTaskTitle == value)
			{
				return;
			}

			_newStandardTaskTitle = value;
			OnPropertyChanged();
		}
	}

	public Task RefreshAsync() => LoadAsync();

	private async Task LoadAsync()
	{
		Tasks.Clear();
		var tasks = await _repository.GetTasksAsync();
		var templates = (await _repository.GetTaskTemplatesAsync(TaskCategory.Mind)).ToList();
		var completedMinutesByTemplateId = tasks
			.Where(t => t.Category == TaskCategory.Mind && t.IsCompleted && t.DueDate.Date <= DateTime.Today && !string.IsNullOrWhiteSpace(t.TaskTemplateId))
			.GroupBy(t => t.TaskTemplateId!)
			.ToDictionary(g => g.Key, g => g.Sum(t => t.DurationMinutes ?? 0));

		foreach (var task in tasks.Where(t => t.Category == TaskCategory.Mind).OrderBy(t => t.DueDate))
		{
			Tasks.Add(task);
		}

		StandardTasks.Clear();
		foreach (var template in templates)
		{
			template.GoalInputMinutes = template.GoalMinutes?.ToString() ?? string.Empty;
			template.CompletedMinutesToday = completedMinutesByTemplateId.TryGetValue(template.Id, out var completedMinutes)
				? completedMinutes
				: 0;
			StandardTasks.Add(template);
		}
		RebuildAddTaskOptions(templates);
	}

	private async Task AddTaskAsync()
	{
		if (_isMutating)
		{
			return;
		}

		if (SelectedAddTaskOption is null)
		{
			return;
		}

		_isMutating = true;
		try
		{
			var duration = ParseOptionalDuration(CompletedTaskDurationMinutes);
			if (duration is null)
			{
				return;
			}

			var isOther = SelectedAddTaskOption.IsOther;
			var title = isOther ? OtherTaskTitle.Trim() : SelectedAddTaskOption.Title;
			if (string.IsNullOrWhiteSpace(title))
			{
				return;
			}

			await _repository.AddTaskAsync(
				title,
				TaskCategory.Mind,
				SelectedDueDate,
				duration,
				isCompleted: true,
				taskTemplateId: isOther ? null : SelectedAddTaskOption.TemplateId);

			OtherTaskTitle = string.Empty;
			CompletedTaskDurationMinutes = string.Empty;
			SelectedDueDate = DateTime.Today;
			await LoadAsync();
		}
		finally
		{
			_isMutating = false;
		}
	}

	private async Task AddStandardTaskAsync()
	{
		if (string.IsNullOrWhiteSpace(NewStandardTaskTitle))
		{
			return;
		}

		await _repository.AddTaskTemplateAsync(NewStandardTaskTitle, TaskCategory.Mind);
		NewStandardTaskTitle = string.Empty;
		await LoadAsync();
	}

	private async Task SaveStandardTaskGoalAsync(TaskTemplateItem? template)
	{
		if (template is null)
		{
			return;
		}

		var goalMinutes = ParseOptionalDuration(template.GoalInputMinutes);
		await _repository.SetTaskTemplateGoalMinutesAsync(template.Id, goalMinutes);
		await LoadAsync();
	}

	private async Task ClearStandardTaskGoalAsync(TaskTemplateItem? template)
	{
		if (template is null)
		{
			return;
		}

		await _repository.SetTaskTemplateGoalMinutesAsync(template.Id, null);
		await LoadAsync();
	}

	private async Task DeleteStandardTaskAsync(TaskTemplateItem? template)
	{
		if (template is null)
		{
			return;
		}

		await _repository.DeleteTaskTemplateAsync(template.Id);
		await LoadAsync();
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

	private static int? ParseOptionalDuration(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : null;
	}

	private void RebuildAddTaskOptions(IReadOnlyList<TaskTemplateItem> templates)
	{
		var previousTemplateId = SelectedAddTaskOption?.TemplateId;
		var previousWasOther = SelectedAddTaskOption?.IsOther == true;

		AddTaskOptions.Clear();
		foreach (var template in templates.OrderBy(t => t.Title))
		{
			AddTaskOptions.Add(new TaskSelectionOption
			{
				TemplateId = template.Id,
				Title = template.Title,
				IsOther = false
			});
		}

		AddTaskOptions.Add(new TaskSelectionOption
		{
			TemplateId = null,
			Title = "Other Task",
			IsOther = true
		});

		SelectedAddTaskOption = AddTaskOptions.FirstOrDefault(option =>
			option.IsOther == previousWasOther &&
			string.Equals(option.TemplateId, previousTemplateId, StringComparison.Ordinal));

		SelectedAddTaskOption ??= AddTaskOptions.FirstOrDefault();
	}
}
