using TriFoldApp.Models;

namespace TriFoldApp.Services;

public interface ITrackerRepository
{
	Task<IReadOnlyList<TrackerTaskItem>> GetTasksAsync();
	Task<IReadOnlyList<WorkoutEntry>> GetWorkoutsAsync();
	Task<IReadOnlyList<WorkoutTemplateDetail>> GetWorkoutTemplatesAsync();
	Task<IReadOnlyList<TaskTemplateItem>> GetTaskTemplatesAsync(TaskCategory category);
	Task<int> GetTaskGoalMinutesAsync(TaskCategory category, int defaultTargetMinutes = 30);
	Task SetTaskGoalMinutesAsync(TaskCategory category, int targetMinutes);
	Task SetTaskTemplateGoalMinutesAsync(string templateId, int? targetMinutes);
	Task AddTaskAsync(string title, TaskCategory category, DateTime dueDate, int? durationMinutes = null, bool isCompleted = false, string? taskTemplateId = null);
	Task ToggleTaskAsync(string taskId);
	Task DeleteTaskAsync(string taskId);
	Task AddTaskTemplateAsync(string title, TaskCategory category, int? durationMinutes = null);
	Task DeleteTaskTemplateAsync(string templateId);
	Task AddWorkoutAsync(string name, int durationMinutes, DateTime completedOn);
	Task AddWorkoutFromTemplateAsync(string templateId, int durationMinutes, DateTime completedOn, IReadOnlyList<WorkoutExerciseLogInput> performedExercises);
	Task DeleteWorkoutAsync(string workoutId);
	Task AddWorkoutTemplateAsync(string templateName, IReadOnlyList<WorkoutTemplateExerciseInput> exercises);
	Task DeleteWorkoutTemplateAsync(string templateId);
	Task<IReadOnlyList<WorkoutTemplateProgressPoint>> GetTemplateProgressAsync(string templateId, int days = 60);
	Task<IReadOnlyList<WorkoutSessionExercise>> GetTemplateSessionExercisesAsync(string templateId, int days = 60);
	Task UpdateWorkoutSessionExerciseAsync(string sessionExerciseId, int sets, int reps, double weight, string weightUnit);
	Task<IReadOnlyList<WorkoutPlanItem>> GetWorkoutPlansAsync(DateTime fromDate, DateTime toDate);
	Task AddWorkoutPlanAsync(string templateId, DateTime plannedDate);
	Task SetWorkoutPlanCompletedAsync(string planId, bool isCompleted);
	Task DeleteWorkoutPlanAsync(string planId);
}
