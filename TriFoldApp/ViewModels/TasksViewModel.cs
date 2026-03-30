using System.Collections.ObjectModel;
using TriFoldApp.Models;
using TriFoldApp.Services;

namespace TriFoldApp.ViewModels;

public class TasksViewModel : BaseViewModel
{
	private readonly ITrackerRepository _repository;
	private string _newTaskTitle = string.Empty;
	private DateTime _selectedDueDate = DateTime.Today;

	public TasksViewModel(ITrackerRepository repository)
	{
		_repository = repository;
		ToggleTaskCommand = new Command<TrackerTaskItem>(task => _ = ToggleTaskAsync(task));
		DeleteTaskCommand = new Command<TrackerTaskItem>(task => _ = DeleteTaskAsync(task));
		AddTaskCommand = new Command(() => _ = AddTaskAsync());
		RefreshCommand = new Command(() => _ = LoadAsync());
		_ = LoadAsync();
	}

	public ObservableCollection<TrackerTaskItem> Tasks { get; } = [];
	public Command<TrackerTaskItem> ToggleTaskCommand { get; }
	public Command<TrackerTaskItem> DeleteTaskCommand { get; }
	public Command AddTaskCommand { get; }
	public Command RefreshCommand { get; }

	public string NewTaskTitle
	{
		get => _newTaskTitle;
		set
		{
			if (_newTaskTitle == value)
			{
				return;
			}

			_newTaskTitle = value;
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

	public int PendingCount => Tasks.Count(t => !t.IsCompleted);

	public Task RefreshAsync() => LoadAsync();

	private async Task LoadAsync()
	{
		Tasks.Clear();
		var tasks = await _repository.GetTasksAsync();
		foreach (var task in tasks
			.Where(t => t.Category == TaskCategory.Daily)
			.OrderBy(t => t.DueDate))
		{
			Tasks.Add(task);
		}

		OnPropertyChanged(nameof(PendingCount));
	}

	private async Task AddTaskAsync()
	{
		if (string.IsNullOrWhiteSpace(NewTaskTitle))
		{
			return;
		}

		await _repository.AddTaskAsync(NewTaskTitle, TaskCategory.Daily, SelectedDueDate);
		NewTaskTitle = string.Empty;
		SelectedDueDate = DateTime.Today;
		await LoadAsync();
	}

	private async Task ToggleTaskAsync(TrackerTaskItem? task)
	{
		if (task is null)
		{
			return;
		}

		await _repository.ToggleTaskAsync(task.Id);
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
}
