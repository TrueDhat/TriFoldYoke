using TriFoldApp.Models;

namespace TriFoldApp.Services;

public class InMemoryTrackerRepository : ITrackerRepository
{
	private readonly List<TrackerTaskItem> _tasks;
	private readonly List<WorkoutEntry> _workouts;
	private readonly List<WorkoutTemplate> _templates;
	private readonly List<WorkoutTemplateExercise> _templateExercises;
	private readonly List<WorkoutSessionExercise> _sessionExercises;
	private readonly List<WorkoutPlanItem> _workoutPlans;
	private readonly List<TaskTemplateItem> _taskTemplates;
	private readonly Dictionary<TaskCategory, int> _taskGoalMinutes;

	public InMemoryTrackerRepository()
	{
		var today = DateTime.Today;

		_tasks =
		[
			new TrackerTaskItem { Title = "Read 15 minutes", Category = TaskCategory.Mind, DueDate = today, DurationMinutes = 15, IsCompleted = false },
			new TrackerTaskItem { Title = "5 minute gratitude", Category = TaskCategory.Spirit, DueDate = today, DurationMinutes = 5, IsCompleted = false },
			new TrackerTaskItem { Title = "Log water intake", Category = TaskCategory.Daily, DueDate = today, IsCompleted = true },
			new TrackerTaskItem { Title = "Deep work block (90m)", Category = TaskCategory.Daily, DueDate = today, IsCompleted = false },
			new TrackerTaskItem { Title = "Evening walk", Category = TaskCategory.Workout, DueDate = today.AddDays(1), IsCompleted = false }
		];

		_workouts =
		[
			new WorkoutEntry { Name = "Upper Body Strength", DurationMinutes = 50, CompletedOn = today.AddDays(-1) },
			new WorkoutEntry { Name = "Zone 2 Cardio", DurationMinutes = 35, CompletedOn = today.AddDays(-2) },
			new WorkoutEntry { Name = "Leg Day", DurationMinutes = 55, CompletedOn = today.AddDays(-4) }
		];

		_templates = [];
		_templateExercises = [];
		_sessionExercises = [];
		_workoutPlans = [];
		_taskTemplates =
		[
			new TaskTemplateItem { Title = "Read 15 minutes", Category = TaskCategory.Mind, DurationMinutes = 15, GoalMinutes = 30 },
			new TaskTemplateItem { Title = "5 minute gratitude", Category = TaskCategory.Spirit, DurationMinutes = 5, GoalMinutes = 15 }
		];
		_taskGoalMinutes = new Dictionary<TaskCategory, int>
		{
			[TaskCategory.Mind] = 30,
			[TaskCategory.Spirit] = 30
		};
	}

	public Task<IReadOnlyList<TrackerTaskItem>> GetTasksAsync()
	{
		return Task.FromResult<IReadOnlyList<TrackerTaskItem>>(_tasks);
	}

	public Task<IReadOnlyList<WorkoutEntry>> GetWorkoutsAsync()
	{
		return Task.FromResult<IReadOnlyList<WorkoutEntry>>(_workouts);
	}

	public Task<IReadOnlyList<WorkoutTemplateDetail>> GetWorkoutTemplatesAsync()
	{
		var details = _templates
			.Select(template => new WorkoutTemplateDetail
			{
				Id = template.Id,
				Name = template.Name,
				Exercises = _templateExercises.Where(e => e.TemplateId == template.Id).ToList()
			})
			.ToList();

		return Task.FromResult<IReadOnlyList<WorkoutTemplateDetail>>(details);
	}

	public Task<IReadOnlyList<TaskTemplateItem>> GetTaskTemplatesAsync(TaskCategory category)
	{
		var templates = _taskTemplates
			.Where(t => t.Category == category)
			.OrderBy(t => t.Title)
			.ToList();
		return Task.FromResult<IReadOnlyList<TaskTemplateItem>>(templates);
	}

	public Task<int> GetTaskGoalMinutesAsync(TaskCategory category, int defaultTargetMinutes = 30)
	{
		if (_taskGoalMinutes.TryGetValue(category, out var targetMinutes))
		{
			return Task.FromResult(targetMinutes);
		}

		return Task.FromResult(defaultTargetMinutes);
	}

	public Task SetTaskGoalMinutesAsync(TaskCategory category, int targetMinutes)
	{
		_taskGoalMinutes[category] = Math.Max(1, targetMinutes);
		return Task.CompletedTask;
	}

	public Task SetTaskTemplateGoalMinutesAsync(string templateId, int? targetMinutes)
	{
		var template = _taskTemplates.FirstOrDefault(t => t.Id == templateId);
		if (template is null)
		{
			return Task.CompletedTask;
		}

		template.GoalMinutes = targetMinutes is > 0 ? targetMinutes : null;
		return Task.CompletedTask;
	}

	public Task AddTaskAsync(string title, TaskCategory category, DateTime dueDate, int? durationMinutes = null, bool isCompleted = false, string? taskTemplateId = null)
	{
		_tasks.Add(new TrackerTaskItem
		{
			Title = title,
			Category = category,
			DueDate = dueDate,
			DurationMinutes = durationMinutes,
			TaskTemplateId = taskTemplateId,
			IsCompleted = isCompleted
		});
		return Task.CompletedTask;
	}

	public Task ToggleTaskAsync(string taskId)
	{
		var task = _tasks.FirstOrDefault(t => t.Id == taskId);
		if (task is not null)
		{
			task.IsCompleted = !task.IsCompleted;
		}

		return Task.CompletedTask;
	}

	public Task DeleteTaskAsync(string taskId)
	{
		var task = _tasks.FirstOrDefault(t => t.Id == taskId);
		if (task is not null)
		{
			_tasks.Remove(task);
		}

		return Task.CompletedTask;
	}

	public Task AddTaskTemplateAsync(string title, TaskCategory category, int? durationMinutes = null)
	{
		if (string.IsNullOrWhiteSpace(title))
		{
			return Task.CompletedTask;
		}

		var normalized = title.Trim();
		var exists = _taskTemplates.Any(t =>
			t.Category == category &&
			string.Equals(t.Title, normalized, StringComparison.OrdinalIgnoreCase) &&
			t.DurationMinutes == durationMinutes);
		if (exists)
		{
			return Task.CompletedTask;
		}

		_taskTemplates.Add(new TaskTemplateItem
		{
			Title = normalized,
			Category = category,
			DurationMinutes = durationMinutes
		});
		return Task.CompletedTask;
	}

	public Task DeleteTaskTemplateAsync(string templateId)
	{
		var template = _taskTemplates.FirstOrDefault(t => t.Id == templateId);
		if (template is not null)
		{
			_taskTemplates.Remove(template);
		}

		return Task.CompletedTask;
	}

	public Task AddWorkoutAsync(string name, int durationMinutes, DateTime completedOn)
	{
		var workout = new WorkoutEntry
		{
			Name = name,
			DurationMinutes = durationMinutes,
			CompletedOn = completedOn
		};
		_workouts.Add(workout);
		return Task.CompletedTask;
	}

	public Task AddWorkoutFromTemplateAsync(string templateId, int durationMinutes, DateTime completedOn, IReadOnlyList<WorkoutExerciseLogInput> performedExercises)
	{
		var template = _templates.FirstOrDefault(t => t.Id == templateId);
		if (template is null || performedExercises.Count == 0)
		{
			return Task.CompletedTask;
		}

		var workout = new WorkoutEntry
		{
			Name = template.Name,
			DurationMinutes = durationMinutes,
			CompletedOn = completedOn
		};
		_workouts.Add(workout);

		foreach (var exercise in performedExercises.Where(e => e.Sets > 0 && e.Reps > 0 && e.Weight >= 0))
		{
			_sessionExercises.Add(new WorkoutSessionExercise
			{
				WorkoutId = workout.Id,
				TemplateId = templateId,
				TemplateExerciseId = exercise.TemplateExerciseId,
				ExerciseName = exercise.ExerciseName,
				Sets = exercise.Sets,
				Reps = exercise.Reps,
				Weight = exercise.Weight,
				WeightUnit = exercise.WeightUnit is "kg" ? "kg" : "lb",
				CompletedOn = completedOn.Date
			});

			var existingExercise = _templateExercises.FirstOrDefault(e => e.Id == exercise.TemplateExerciseId);
			if (existingExercise is not null && exercise.Weight > existingExercise.Weight)
			{
				existingExercise.Weight = exercise.Weight;
				existingExercise.WeightUnit = exercise.WeightUnit is "kg" ? "kg" : "lb";
			}
		}

		return Task.CompletedTask;
	}

	public Task DeleteWorkoutAsync(string workoutId)
	{
		var workout = _workouts.FirstOrDefault(w => w.Id == workoutId);
		if (workout is not null)
		{
			_workouts.Remove(workout);
		}

		_sessionExercises.RemoveAll(e => e.WorkoutId == workoutId);

		return Task.CompletedTask;
	}

	public Task AddWorkoutTemplateAsync(string templateName, IReadOnlyList<WorkoutTemplateExerciseInput> exercises)
	{
		var template = new WorkoutTemplate
		{
			Name = templateName.Trim(),
			CreatedOn = DateTime.UtcNow
		};
		_templates.Add(template);

		foreach (var exercise in exercises)
		{
			_templateExercises.Add(new WorkoutTemplateExercise
			{
				TemplateId = template.Id,
				Name = exercise.Name.Trim(),
				Sets = exercise.Sets,
				Reps = exercise.Reps,
				Weight = exercise.Weight,
				WeightUnit = exercise.WeightUnit is "kg" ? "kg" : "lb"
			});
		}

		return Task.CompletedTask;
	}

	public Task DeleteWorkoutTemplateAsync(string templateId)
	{
		var template = _templates.FirstOrDefault(t => t.Id == templateId);
		if (template is not null)
		{
			_templates.Remove(template);
		}

		_templateExercises.RemoveAll(e => e.TemplateId == templateId);
		_sessionExercises.RemoveAll(e => e.TemplateId == templateId);
		_workoutPlans.RemoveAll(p => p.TemplateId == templateId);
		return Task.CompletedTask;
	}

	public Task<IReadOnlyList<WorkoutTemplateProgressPoint>> GetTemplateProgressAsync(string templateId, int days = 60)
	{
		var startDay = DateTime.Today.AddDays(-(Math.Max(1, days) - 1));
		var points = _sessionExercises
			.Where(e => e.TemplateId == templateId && e.CompletedOn.Date >= startDay)
			.GroupBy(e => e.CompletedOn.Date)
			.Select(group => new WorkoutTemplateProgressPoint
			{
				Day = group.Key,
				TotalLifted = group.Sum(e => e.Weight),
				MaxSets = group.Max(e => e.Sets)
			})
			.OrderBy(p => p.Day)
			.ToList();

		return Task.FromResult<IReadOnlyList<WorkoutTemplateProgressPoint>>(points);
	}

	public Task<IReadOnlyList<WorkoutSessionExercise>> GetTemplateSessionExercisesAsync(string templateId, int days = 60)
	{
		var startDay = DateTime.Today.AddDays(-(Math.Max(1, days) - 1));
		var rows = _sessionExercises
			.Where(e => e.TemplateId == templateId && e.CompletedOn.Date >= startDay)
			.OrderBy(e => e.CompletedOn)
			.ThenBy(e => e.ExerciseName)
			.ToList();
		return Task.FromResult<IReadOnlyList<WorkoutSessionExercise>>(rows);
	}

	public Task UpdateWorkoutSessionExerciseAsync(string sessionExerciseId, int sets, int reps, double weight, string weightUnit)
	{
		var row = _sessionExercises.FirstOrDefault(e => e.Id == sessionExerciseId);
		if (row is null)
		{
			return Task.CompletedTask;
		}

		row.Sets = Math.Max(1, sets);
		row.Reps = Math.Max(1, reps);
		row.Weight = Math.Max(0, weight);
		row.WeightUnit = weightUnit is "kg" ? "kg" : "lb";

		if (!string.IsNullOrWhiteSpace(row.TemplateExerciseId))
		{
			var templateExercise = _templateExercises.FirstOrDefault(e => e.Id == row.TemplateExerciseId);
			if (templateExercise is not null && row.Weight > templateExercise.Weight)
			{
				templateExercise.Weight = row.Weight;
				templateExercise.WeightUnit = row.WeightUnit;
			}
		}

		return Task.CompletedTask;
	}

	public Task<IReadOnlyList<WorkoutPlanItem>> GetWorkoutPlansAsync(DateTime fromDate, DateTime toDate)
	{
		var from = fromDate.Date;
		var to = toDate.Date;
		var plans = _workoutPlans
			.Where(p => p.PlannedDate.Date >= from && p.PlannedDate.Date <= to)
			.OrderBy(p => p.PlannedDate)
			.ThenBy(p => p.TemplateName)
			.ToList();
		return Task.FromResult<IReadOnlyList<WorkoutPlanItem>>(plans);
	}

	public Task AddWorkoutPlanAsync(string templateId, DateTime plannedDate)
	{
		var template = _templates.FirstOrDefault(t => t.Id == templateId);
		if (template is null)
		{
			return Task.CompletedTask;
		}

		var exists = _workoutPlans.Any(p => p.TemplateId == templateId && p.PlannedDate.Date == plannedDate.Date);
		if (exists)
		{
			return Task.CompletedTask;
		}

		_workoutPlans.Add(new WorkoutPlanItem
		{
			TemplateId = templateId,
			TemplateName = template.Name,
			PlannedDate = plannedDate.Date,
			IsCompleted = false,
			CompletedWorkoutId = null
		});
		return Task.CompletedTask;
	}

	public Task SetWorkoutPlanCompletedAsync(string planId, bool isCompleted)
	{
		var plan = _workoutPlans.FirstOrDefault(p => p.Id == planId);
		if (plan is null)
		{
			return Task.CompletedTask;
		}

		if (isCompleted)
		{
			if (!string.IsNullOrWhiteSpace(plan.CompletedWorkoutId))
			{
				plan.IsCompleted = true;
				return Task.CompletedTask;
			}

			var template = _templates.FirstOrDefault(t => t.Id == plan.TemplateId);
			if (template is null)
			{
				return Task.CompletedTask;
			}

			var workout = new WorkoutEntry
			{
				Name = template.Name,
				DurationMinutes = 0,
				CompletedOn = plan.PlannedDate.Date
			};
			_workouts.Add(workout);

			var exercises = _templateExercises
				.Where(e => e.TemplateId == template.Id)
				.OrderBy(e => e.Name)
				.ToList();
			foreach (var exercise in exercises)
			{
				_sessionExercises.Add(new WorkoutSessionExercise
				{
					WorkoutId = workout.Id,
					TemplateId = template.Id,
					TemplateExerciseId = exercise.Id,
					ExerciseName = exercise.Name,
					Sets = exercise.Sets,
					Reps = exercise.Reps,
					Weight = exercise.Weight,
					WeightUnit = exercise.WeightUnit,
					CompletedOn = plan.PlannedDate.Date
				});
			}

			plan.IsCompleted = true;
			plan.CompletedWorkoutId = workout.Id;
			return Task.CompletedTask;
		}

		if (!string.IsNullOrWhiteSpace(plan.CompletedWorkoutId))
		{
			var workout = _workouts.FirstOrDefault(w => w.Id == plan.CompletedWorkoutId);
			if (workout is not null)
			{
				_workouts.Remove(workout);
			}
			_sessionExercises.RemoveAll(e => e.WorkoutId == plan.CompletedWorkoutId);
		}

		plan.IsCompleted = false;
		plan.CompletedWorkoutId = null;
		return Task.CompletedTask;
	}

	public Task DeleteWorkoutPlanAsync(string planId)
	{
		var plan = _workoutPlans.FirstOrDefault(p => p.Id == planId);
		if (plan is not null)
		{
			if (!string.IsNullOrWhiteSpace(plan.CompletedWorkoutId))
			{
				var workout = _workouts.FirstOrDefault(w => w.Id == plan.CompletedWorkoutId);
				if (workout is not null)
				{
					_workouts.Remove(workout);
				}
				_sessionExercises.RemoveAll(e => e.WorkoutId == plan.CompletedWorkoutId);
			}
			_workoutPlans.Remove(plan);
		}

		return Task.CompletedTask;
	}
}
