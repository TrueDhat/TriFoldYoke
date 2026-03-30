using SQLite;
using TriFoldApp.Models;

namespace TriFoldApp.Services;

public class SQLiteTrackerRepository : ITrackerRepository
{
	private readonly SQLiteAsyncConnection _database;
	private readonly Lazy<Task> _initializer;

	public SQLiteTrackerRepository()
	{
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "trifold.db3");
		_database = new SQLiteAsyncConnection(dbPath);
		_initializer = new Lazy<Task>(InitializeAsync);
	}

	public async Task<IReadOnlyList<TrackerTaskItem>> GetTasksAsync()
	{
		await EnsureInitializedAsync();
		return await _database.Table<TrackerTaskItem>().OrderBy(t => t.DueDate).ToListAsync();
	}

	public async Task<IReadOnlyList<WorkoutEntry>> GetWorkoutsAsync()
	{
		await EnsureInitializedAsync();
		return await _database.Table<WorkoutEntry>().OrderByDescending(w => w.CompletedOn).ToListAsync();
	}

	public async Task<IReadOnlyList<WorkoutTemplateDetail>> GetWorkoutTemplatesAsync()
	{
		await EnsureInitializedAsync();

		var templates = await _database.Table<WorkoutTemplate>()
			.OrderByDescending(t => t.CreatedOn)
			.ToListAsync();
		var allExercises = await _database.Table<WorkoutTemplateExercise>().ToListAsync();

		return templates.Select(template => new WorkoutTemplateDetail
		{
			Id = template.Id,
			Name = template.Name,
			Exercises = allExercises
				.Where(e => e.TemplateId == template.Id)
				.OrderBy(e => e.Name)
				.ToList()
		}).ToList();
	}

	public async Task<IReadOnlyList<TaskTemplateItem>> GetTaskTemplatesAsync(TaskCategory category)
	{
		await EnsureInitializedAsync();
		return await _database.Table<TaskTemplateItem>()
			.Where(t => t.Category == category)
			.OrderBy(t => t.Title)
			.ToListAsync();
	}

	public async Task<int> GetTaskGoalMinutesAsync(TaskCategory category, int defaultTargetMinutes = 30)
	{
		await EnsureInitializedAsync();
		var goal = await _database.FindAsync<TaskGoalItem>(category);
		return goal?.TargetMinutes ?? defaultTargetMinutes;
	}

	public async Task SetTaskGoalMinutesAsync(TaskCategory category, int targetMinutes)
	{
		await EnsureInitializedAsync();
		var clamped = Math.Max(1, targetMinutes);
		var existing = await _database.FindAsync<TaskGoalItem>(category);
		if (existing is null)
		{
			await _database.InsertAsync(new TaskGoalItem
			{
				Category = category,
				TargetMinutes = clamped
			});
			return;
		}

		existing.TargetMinutes = clamped;
		await _database.UpdateAsync(existing);
	}

	public async Task SetTaskTemplateGoalMinutesAsync(string templateId, int? targetMinutes)
	{
		await EnsureInitializedAsync();
		var template = await _database.FindAsync<TaskTemplateItem>(templateId);
		if (template is null)
		{
			return;
		}

		template.GoalMinutes = targetMinutes is > 0 ? targetMinutes : null;
		await _database.UpdateAsync(template);
	}

	public async Task AddTaskAsync(string title, TaskCategory category, DateTime dueDate, int? durationMinutes = null, bool isCompleted = false, string? taskTemplateId = null)
	{
		await EnsureInitializedAsync();
		var task = new TrackerTaskItem
		{
			Title = title.Trim(),
			Category = category,
			DueDate = dueDate.Date,
			DurationMinutes = durationMinutes,
			TaskTemplateId = taskTemplateId,
			IsCompleted = isCompleted
		};
		await _database.InsertAsync(task);
	}

	public async Task ToggleTaskAsync(string taskId)
	{
		await EnsureInitializedAsync();
		var task = await _database.FindAsync<TrackerTaskItem>(taskId);
		if (task is null)
		{
			return;
		}

		task.IsCompleted = !task.IsCompleted;
		await _database.UpdateAsync(task);
	}

	public async Task DeleteTaskAsync(string taskId)
	{
		await EnsureInitializedAsync();
		await _database.DeleteAsync<TrackerTaskItem>(taskId);
	}

	public async Task AddTaskTemplateAsync(string title, TaskCategory category, int? durationMinutes = null)
	{
		await EnsureInitializedAsync();
		if (string.IsNullOrWhiteSpace(title))
		{
			return;
		}

		var normalized = title.Trim();
		var existingTemplates = await _database.Table<TaskTemplateItem>()
			.Where(t => t.Category == category)
			.ToListAsync();

		var alreadyExists = existingTemplates.Any(t =>
			string.Equals(t.Title, normalized, StringComparison.OrdinalIgnoreCase) &&
			t.DurationMinutes == durationMinutes);

		if (alreadyExists)
		{
			return;
		}

		await _database.InsertAsync(new TaskTemplateItem
		{
			Title = normalized,
			Category = category,
			DurationMinutes = durationMinutes
		});
	}

	public async Task DeleteTaskTemplateAsync(string templateId)
	{
		await EnsureInitializedAsync();
		await _database.DeleteAsync<TaskTemplateItem>(templateId);
	}

	public async Task AddWorkoutAsync(string name, int durationMinutes, DateTime completedOn)
	{
		await EnsureInitializedAsync();
		var workout = new WorkoutEntry
		{
			Name = name.Trim(),
			DurationMinutes = durationMinutes,
			CompletedOn = completedOn.Date
		};
		await _database.InsertAsync(workout);
	}

	public async Task AddWorkoutFromTemplateAsync(string templateId, int durationMinutes, DateTime completedOn, IReadOnlyList<WorkoutExerciseLogInput> performedExercises)
	{
		await EnsureInitializedAsync();
		var template = await _database.FindAsync<WorkoutTemplate>(templateId);
		if (template is null || performedExercises.Count == 0)
		{
			return;
		}

		var normalized = performedExercises
			.Where(e => e.Sets > 0 && e.Reps > 0 && e.Weight >= 0)
			.Select(e => new WorkoutExerciseLogInput
			{
				TemplateExerciseId = e.TemplateExerciseId,
				ExerciseName = e.ExerciseName.Trim(),
				Sets = e.Sets,
				Reps = e.Reps,
				Weight = e.Weight,
				WeightUnit = e.WeightUnit is "kg" ? "kg" : "lb"
			})
			.ToList();

		if (normalized.Count == 0)
		{
			return;
		}

		var workout = new WorkoutEntry
		{
			Name = template.Name,
			DurationMinutes = durationMinutes,
			CompletedOn = completedOn.Date
		};

		await _database.RunInTransactionAsync(connection =>
		{
			connection.Insert(workout);

			foreach (var exercise in normalized)
			{
				connection.Insert(new WorkoutSessionExercise
				{
					WorkoutId = workout.Id,
					TemplateId = templateId,
					TemplateExerciseId = exercise.TemplateExerciseId,
					ExerciseName = exercise.ExerciseName,
					Sets = exercise.Sets,
					Reps = exercise.Reps,
					Weight = exercise.Weight,
					WeightUnit = exercise.WeightUnit,
					CompletedOn = completedOn.Date
				});

				if (string.IsNullOrWhiteSpace(exercise.TemplateExerciseId))
				{
					continue;
				}

				var existing = connection.Find<WorkoutTemplateExercise>(exercise.TemplateExerciseId);
				if (existing is not null && exercise.Weight > existing.Weight)
				{
					existing.Weight = exercise.Weight;
					existing.WeightUnit = exercise.WeightUnit;
					connection.Update(existing);
				}
			}
		});
	}

	public async Task DeleteWorkoutAsync(string workoutId)
	{
		await EnsureInitializedAsync();
		await _database.RunInTransactionAsync(connection =>
		{
			connection.Delete<WorkoutEntry>(workoutId);
			var sessionRows = connection.Table<WorkoutSessionExercise>()
				.Where(e => e.WorkoutId == workoutId)
				.ToList();
			foreach (var row in sessionRows)
			{
				connection.Delete(row);
			}
		});
	}

	public async Task AddWorkoutTemplateAsync(string templateName, IReadOnlyList<WorkoutTemplateExerciseInput> exercises)
	{
		await EnsureInitializedAsync();

		if (string.IsNullOrWhiteSpace(templateName) || exercises.Count == 0)
		{
			return;
		}

		var template = new WorkoutTemplate
		{
			Name = templateName.Trim(),
			CreatedOn = DateTime.UtcNow
		};

		await _database.RunInTransactionAsync(connection =>
		{
			connection.Insert(template);
			foreach (var exercise in exercises)
			{
				connection.Insert(new WorkoutTemplateExercise
				{
					TemplateId = template.Id,
					Name = exercise.Name.Trim(),
					Sets = exercise.Sets,
					Reps = exercise.Reps,
					Weight = exercise.Weight,
					WeightUnit = exercise.WeightUnit is "kg" ? "kg" : "lb"
				});
			}
		});
	}

	public async Task DeleteWorkoutTemplateAsync(string templateId)
	{
		await EnsureInitializedAsync();

		await _database.RunInTransactionAsync(connection =>
		{
			connection.Delete<WorkoutTemplate>(templateId);
			var children = connection.Table<WorkoutTemplateExercise>()
				.Where(e => e.TemplateId == templateId)
				.ToList();
			foreach (var exercise in children)
			{
				connection.Delete(exercise);
			}
			var sessionRows = connection.Table<WorkoutSessionExercise>()
				.Where(e => e.TemplateId == templateId)
				.ToList();
			foreach (var row in sessionRows)
			{
				connection.Delete(row);
			}
			var planRows = connection.Table<WorkoutPlanItem>()
				.Where(p => p.TemplateId == templateId)
				.ToList();
			foreach (var row in planRows)
			{
				connection.Delete(row);
			}
		});
	}

	public async Task<IReadOnlyList<WorkoutTemplateProgressPoint>> GetTemplateProgressAsync(string templateId, int days = 60)
	{
		await EnsureInitializedAsync();
		var startDay = DateTime.Today.AddDays(-(Math.Max(1, days) - 1));
		var rows = await _database.Table<WorkoutSessionExercise>()
			.Where(e => e.TemplateId == templateId && e.CompletedOn >= startDay)
			.ToListAsync();

		return rows
			.GroupBy(e => e.CompletedOn.Date)
			.Select(group => new WorkoutTemplateProgressPoint
			{
				Day = group.Key,
				TotalLifted = group.Sum(e => e.Weight),
				MaxSets = group.Max(e => e.Sets)
			})
			.OrderBy(p => p.Day)
			.ToList();
	}

	public async Task<IReadOnlyList<WorkoutSessionExercise>> GetTemplateSessionExercisesAsync(string templateId, int days = 60)
	{
		await EnsureInitializedAsync();
		var startDay = DateTime.Today.AddDays(-(Math.Max(1, days) - 1));
		return await _database.Table<WorkoutSessionExercise>()
			.Where(e => e.TemplateId == templateId && e.CompletedOn >= startDay)
			.OrderBy(e => e.CompletedOn)
			.ThenBy(e => e.ExerciseName)
			.ToListAsync();
	}

	public async Task UpdateWorkoutSessionExerciseAsync(string sessionExerciseId, int sets, int reps, double weight, string weightUnit)
	{
		await EnsureInitializedAsync();
		var row = await _database.FindAsync<WorkoutSessionExercise>(sessionExerciseId);
		if (row is null)
		{
			return;
		}

		row.Sets = Math.Max(1, sets);
		row.Reps = Math.Max(1, reps);
		row.Weight = Math.Max(0, weight);
		row.WeightUnit = weightUnit is "kg" ? "kg" : "lb";
		await _database.UpdateAsync(row);

		if (string.IsNullOrWhiteSpace(row.TemplateExerciseId))
		{
			return;
		}

		var templateExercise = await _database.FindAsync<WorkoutTemplateExercise>(row.TemplateExerciseId);
		if (templateExercise is not null && row.Weight > templateExercise.Weight)
		{
			templateExercise.Weight = row.Weight;
			templateExercise.WeightUnit = row.WeightUnit;
			await _database.UpdateAsync(templateExercise);
		}
	}

	public async Task<IReadOnlyList<WorkoutPlanItem>> GetWorkoutPlansAsync(DateTime fromDate, DateTime toDate)
	{
		await EnsureInitializedAsync();
		var from = fromDate.Date;
		var to = toDate.Date;
		return await _database.Table<WorkoutPlanItem>()
			.Where(p => p.PlannedDate >= from && p.PlannedDate <= to)
			.OrderBy(p => p.PlannedDate)
			.ThenBy(p => p.TemplateName)
			.ToListAsync();
	}

	public async Task AddWorkoutPlanAsync(string templateId, DateTime plannedDate)
	{
		await EnsureInitializedAsync();
		var template = await _database.FindAsync<WorkoutTemplate>(templateId);
		if (template is null)
		{
			return;
		}

		var date = plannedDate.Date;
		var exists = await _database.Table<WorkoutPlanItem>()
			.Where(p => p.TemplateId == templateId && p.PlannedDate == date)
			.FirstOrDefaultAsync();
		if (exists is not null)
		{
			return;
		}

		await _database.InsertAsync(new WorkoutPlanItem
		{
			TemplateId = templateId,
			TemplateName = template.Name,
			PlannedDate = date,
			IsCompleted = false,
			CompletedWorkoutId = null
		});
	}

	public async Task SetWorkoutPlanCompletedAsync(string planId, bool isCompleted)
	{
		await EnsureInitializedAsync();
		var plan = await _database.FindAsync<WorkoutPlanItem>(planId);
		if (plan is null)
		{
			return;
		}

		if (isCompleted)
		{
			if (!string.IsNullOrWhiteSpace(plan.CompletedWorkoutId))
			{
				plan.IsCompleted = true;
				await _database.UpdateAsync(plan);
				return;
			}

			var template = await _database.FindAsync<WorkoutTemplate>(plan.TemplateId);
			if (template is null)
			{
				return;
			}

			var templateExercises = await _database.Table<WorkoutTemplateExercise>()
				.Where(e => e.TemplateId == template.Id)
				.ToListAsync();
			if (templateExercises.Count == 0)
			{
				return;
			}

			var workout = new WorkoutEntry
			{
				Name = template.Name,
				DurationMinutes = 0,
				CompletedOn = plan.PlannedDate.Date
			};

			await _database.RunInTransactionAsync(connection =>
			{
				connection.Insert(workout);
				foreach (var exercise in templateExercises)
				{
					connection.Insert(new WorkoutSessionExercise
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
				connection.Update(plan);
			});
			return;
		}

		await _database.RunInTransactionAsync(connection =>
		{
			if (!string.IsNullOrWhiteSpace(plan.CompletedWorkoutId))
			{
				connection.Delete<WorkoutEntry>(plan.CompletedWorkoutId);
				var sessionRows = connection.Table<WorkoutSessionExercise>()
					.Where(e => e.WorkoutId == plan.CompletedWorkoutId)
					.ToList();
				foreach (var row in sessionRows)
				{
					connection.Delete(row);
				}
			}

			plan.IsCompleted = false;
			plan.CompletedWorkoutId = null;
			connection.Update(plan);
		});
	}

	public async Task DeleteWorkoutPlanAsync(string planId)
	{
		await EnsureInitializedAsync();
		await _database.RunInTransactionAsync(connection =>
		{
			var plan = connection.Find<WorkoutPlanItem>(planId);
			if (plan is null)
			{
				return;
			}

			if (!string.IsNullOrWhiteSpace(plan.CompletedWorkoutId))
			{
				connection.Delete<WorkoutEntry>(plan.CompletedWorkoutId);
				var sessionRows = connection.Table<WorkoutSessionExercise>()
					.Where(e => e.WorkoutId == plan.CompletedWorkoutId)
					.ToList();
				foreach (var row in sessionRows)
				{
					connection.Delete(row);
				}
			}

			connection.Delete(plan);
		});
	}

	private async Task EnsureInitializedAsync()
	{
		await _initializer.Value;
	}

	private async Task InitializeAsync()
	{
		await _database.CreateTableAsync<TrackerTaskItem>();
		await _database.CreateTableAsync<WorkoutEntry>();
		await _database.CreateTableAsync<WorkoutTemplate>();
		await _database.CreateTableAsync<WorkoutTemplateExercise>();
		await _database.CreateTableAsync<WorkoutSessionExercise>();
		await _database.CreateTableAsync<WorkoutPlanItem>();
		await _database.CreateTableAsync<TaskTemplateItem>();
		await _database.CreateTableAsync<TaskGoalItem>();
		await EnsureWorkoutTemplateExerciseColumnsAsync();
		await EnsureWorkoutPlanColumnsAsync();
		await EnsureWorkoutPlanUniquenessAsync();
		await EnsureTaskColumnsAsync();
		await EnsureTaskTemplateColumnsAsync();
		await EnsureTaskGoalColumnsAsync();
		await _database.ExecuteAsync("DELETE FROM tasks WHERE Title = ?", "Morning mobility");

		var taskCount = await _database.Table<TrackerTaskItem>().CountAsync();
		if (taskCount > 0)
		{
			return;
		}

		var today = DateTime.Today;
		var seedTasks = new[]
		{
			new TrackerTaskItem { Title = "Read 15 minutes", Category = TaskCategory.Mind, DueDate = today, DurationMinutes = 15, IsCompleted = false },
			new TrackerTaskItem { Title = "5 minute gratitude", Category = TaskCategory.Spirit, DueDate = today, DurationMinutes = 5, IsCompleted = false },
			new TrackerTaskItem { Title = "Log water intake", Category = TaskCategory.Daily, DueDate = today, IsCompleted = true },
			new TrackerTaskItem { Title = "Evening walk", Category = TaskCategory.Workout, DueDate = today.AddDays(1), IsCompleted = false }
		};
		var seedWorkouts = new[]
		{
			new WorkoutEntry { Name = "Upper Body Strength", DurationMinutes = 50, CompletedOn = today.AddDays(-1) },
			new WorkoutEntry { Name = "Zone 2 Cardio", DurationMinutes = 35, CompletedOn = today.AddDays(-2) },
			new WorkoutEntry { Name = "Leg Day", DurationMinutes = 55, CompletedOn = today.AddDays(-4) }
		};
		var template = new WorkoutTemplate
		{
			Name = "Push Day",
			CreatedOn = DateTime.UtcNow
		};
		var templateExercises = new[]
		{
			new WorkoutTemplateExercise { TemplateId = template.Id, Name = "Bench Press", Sets = 4, Reps = 8, Weight = 135, WeightUnit = "lb" },
			new WorkoutTemplateExercise { TemplateId = template.Id, Name = "Incline Dumbbell Press", Sets = 3, Reps = 10, Weight = 50, WeightUnit = "lb" },
			new WorkoutTemplateExercise { TemplateId = template.Id, Name = "Tricep Pushdown", Sets = 3, Reps = 12, Weight = 42.5, WeightUnit = "lb" }
		};
		var taskTemplates = new[]
		{
			new TaskTemplateItem { Title = "Read 15 minutes", Category = TaskCategory.Mind, DurationMinutes = 15, GoalMinutes = 30 },
			new TaskTemplateItem { Title = "5 minute gratitude", Category = TaskCategory.Spirit, DurationMinutes = 5, GoalMinutes = 15 }
		};

		await _database.InsertAllAsync(seedTasks);
		await _database.InsertAllAsync(seedWorkouts);
		await _database.InsertAsync(template);
		await _database.InsertAllAsync(templateExercises);
		await _database.InsertAllAsync(taskTemplates);
		await _database.InsertAllAsync(new[]
		{
			new TaskGoalItem { Category = TaskCategory.Mind, TargetMinutes = 30 },
			new TaskGoalItem { Category = TaskCategory.Spirit, TargetMinutes = 30 }
		});
	}

	private async Task EnsureWorkoutTemplateExerciseColumnsAsync()
	{
		var columns = await _database.QueryAsync<SqliteTableInfoRow>("PRAGMA table_info(workout_template_exercises);");
		var hasWeight = columns.Any(c => string.Equals(c.name, "Weight", StringComparison.OrdinalIgnoreCase));
		var hasWeightUnit = columns.Any(c => string.Equals(c.name, "WeightUnit", StringComparison.OrdinalIgnoreCase));

		if (!hasWeight)
		{
			await _database.ExecuteAsync("ALTER TABLE workout_template_exercises ADD COLUMN Weight REAL NOT NULL DEFAULT 0;");
		}

		if (!hasWeightUnit)
		{
			await _database.ExecuteAsync("ALTER TABLE workout_template_exercises ADD COLUMN WeightUnit TEXT NOT NULL DEFAULT 'lb';");
		}
	}

	private async Task EnsureTaskColumnsAsync()
	{
		var columns = await _database.QueryAsync<SqliteTableInfoRow>("PRAGMA table_info(tasks);");
		var hasDurationMinutes = columns.Any(c => string.Equals(c.name, "DurationMinutes", StringComparison.OrdinalIgnoreCase));
		var hasTaskTemplateId = columns.Any(c => string.Equals(c.name, "TaskTemplateId", StringComparison.OrdinalIgnoreCase));
		if (!hasDurationMinutes)
		{
			await _database.ExecuteAsync("ALTER TABLE tasks ADD COLUMN DurationMinutes INTEGER NULL;");
		}

		if (!hasTaskTemplateId)
		{
			await _database.ExecuteAsync("ALTER TABLE tasks ADD COLUMN TaskTemplateId TEXT NULL;");
		}
	}

	private async Task EnsureWorkoutPlanUniquenessAsync()
	{
		// Remove historical duplicates so the unique index can be created safely.
		await _database.ExecuteAsync(
			"""
			DELETE FROM workout_plans
			WHERE rowid NOT IN
			(
				SELECT MIN(rowid)
				FROM workout_plans
				GROUP BY TemplateId, PlannedDate
			);
			""");

		await _database.ExecuteAsync(
			"CREATE UNIQUE INDEX IF NOT EXISTS ux_workout_plans_template_date ON workout_plans(TemplateId, PlannedDate);");
	}

	private async Task EnsureWorkoutPlanColumnsAsync()
	{
		var columns = await _database.QueryAsync<SqliteTableInfoRow>("PRAGMA table_info(workout_plans);");
		var hasCompletedWorkoutId = columns.Any(c => string.Equals(c.name, "CompletedWorkoutId", StringComparison.OrdinalIgnoreCase));
		if (!hasCompletedWorkoutId)
		{
			await _database.ExecuteAsync("ALTER TABLE workout_plans ADD COLUMN CompletedWorkoutId TEXT NULL;");
		}
	}

	private async Task EnsureTaskTemplateColumnsAsync()
	{
		var columns = await _database.QueryAsync<SqliteTableInfoRow>("PRAGMA table_info(task_templates);");
		var hasDurationMinutes = columns.Any(c => string.Equals(c.name, "DurationMinutes", StringComparison.OrdinalIgnoreCase));
		var hasGoalMinutes = columns.Any(c => string.Equals(c.name, "GoalMinutes", StringComparison.OrdinalIgnoreCase));
		if (!hasDurationMinutes)
		{
			await _database.ExecuteAsync("ALTER TABLE task_templates ADD COLUMN DurationMinutes INTEGER NULL;");
		}

		if (!hasGoalMinutes)
		{
			await _database.ExecuteAsync("ALTER TABLE task_templates ADD COLUMN GoalMinutes INTEGER NULL;");
		}
	}

	private async Task EnsureTaskGoalColumnsAsync()
	{
		var columns = await _database.QueryAsync<SqliteTableInfoRow>("PRAGMA table_info(task_goals);");
		var hasTargetMinutes = columns.Any(c => string.Equals(c.name, "TargetMinutes", StringComparison.OrdinalIgnoreCase));
		var hasTargetCount = columns.Any(c => string.Equals(c.name, "TargetCount", StringComparison.OrdinalIgnoreCase));

		if (!hasTargetMinutes)
		{
			await _database.ExecuteAsync("ALTER TABLE task_goals ADD COLUMN TargetMinutes INTEGER NOT NULL DEFAULT 30;");
		}

		if (hasTargetCount)
		{
			await _database.ExecuteAsync("UPDATE task_goals SET TargetMinutes = TargetCount WHERE TargetCount > 0;");
		}
	}

	private sealed class SqliteTableInfoRow
	{
		public string name { get; set; } = string.Empty;
	}
}
