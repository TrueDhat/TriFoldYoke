using System.Collections.ObjectModel;
using System.Globalization;
using System.Security;
using System.Text;
using TriFoldApp.Models;
using TriFoldApp.Services;

namespace TriFoldApp.ViewModels;

public class WorkoutsViewModel : BaseViewModel
{
	private readonly ITrackerRepository _repository;
	private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
	private readonly SemaphoreSlim _planLoadSemaphore = new(1, 1);
	private WorkoutSelectionOption? _selectedWorkoutOption;
	private WorkoutTemplateDetail? _selectedProgressTemplate;
	private WorkoutTemplateDetail? _selectedPlanTemplate;
	private string _otherWorkoutName = string.Empty;
	private string _newWorkoutMinutes = string.Empty;
	private DateTime _selectedCompletedOn = DateTime.Today;
	private string _templateName = string.Empty;
	private DateTime _selectedPlanDate = DateTime.Today;
	private DateTime _displayMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
	private DateTime _selectedTemplateQuickAddDate = DateTime.Today;
	private DateTime? _editingGraphDay;
	private bool _isAddingPlan;
	private HtmlWebViewSource _templateProgressChartSource = new() { Html = "<html><body>No data</body></html>" };

	public WorkoutsViewModel(ITrackerRepository repository)
	{
		_repository = repository;
		DeleteWorkoutCommand = new Command<WorkoutEntry>(workout => _ = DeleteWorkoutAsync(workout));
		AddWorkoutCommand = new Command(() => _ = AddWorkoutAsync());
		AddTemplateExerciseCommand = new Command(AddTemplateExerciseRow);
		RemoveTemplateExerciseCommand = new Command<WorkoutTemplateExerciseDraft>(RemoveTemplateExerciseRow);
		SaveTemplateCommand = new Command(() => _ = SaveTemplateAsync());
		UseTemplateCommand = new Command<WorkoutTemplateDetail>(UseTemplate);
		QuickAddTemplateWorkoutCommand = new Command<WorkoutTemplateDetail>(template => _ = QuickAddTemplateWorkoutAsync(template));
		DeleteTemplateCommand = new Command<WorkoutTemplateDetail>(template => _ = DeleteTemplateAsync(template));
		AddPlanCommand = new Command(() => _ = AddPlanAsync());
		TogglePlanCompletionCommand = new Command<WorkoutPlanItem>(plan => _ = TogglePlanCompletionAsync(plan));
		DeletePlanCommand = new Command<WorkoutPlanItem>(plan => _ = DeletePlanAsync(plan));
		RefreshGraphCommand = new Command(() => _ = UpdateTemplateProgressChartAsync());
		SaveGraphPointEditsCommand = new Command(() => _ = SaveGraphPointEditsAsync());
		CancelGraphPointEditsCommand = new Command(CancelGraphPointEdits);
		PreviousMonthCommand = new Command(() => _ = MoveMonthAsync(-1));
		NextMonthCommand = new Command(() => _ = MoveMonthAsync(1));
		SelectCalendarDayCommand = new Command<WorkoutCalendarDay>(day => _ = SelectCalendarDayAsync(day));
		RefreshCommand = new Command(() => _ = LoadAsync());
		AddTemplateExerciseRow();
		_ = LoadAsync();
	}

	public ObservableCollection<WorkoutEntry> Workouts { get; } = [];
	public ObservableCollection<WorkoutTemplateDetail> WorkoutTemplates { get; } = [];
	public ObservableCollection<WorkoutTemplateExerciseDraft> TemplateExerciseRows { get; } = [];
	public ObservableCollection<WorkoutExerciseLogDraft> ActiveTemplateExerciseLogs { get; } = [];
	public ObservableCollection<WorkoutExerciseLogDraft> GraphPointExerciseEdits { get; } = [];
	public ObservableCollection<WorkoutSelectionOption> AddWorkoutOptions { get; } = [];
	public ObservableCollection<WorkoutPlanItem> WorkoutPlansForSelectedDate { get; } = [];
	public ObservableCollection<WorkoutCalendarDay> CalendarDays { get; } = [];
	public IReadOnlyList<string> WeightUnits { get; } = ["lb", "kg"];
	public Command<WorkoutEntry> DeleteWorkoutCommand { get; }
	public Command AddWorkoutCommand { get; }
	public Command AddTemplateExerciseCommand { get; }
	public Command<WorkoutTemplateExerciseDraft> RemoveTemplateExerciseCommand { get; }
	public Command SaveTemplateCommand { get; }
	public Command<WorkoutTemplateDetail> UseTemplateCommand { get; }
	public Command<WorkoutTemplateDetail> QuickAddTemplateWorkoutCommand { get; }
	public Command<WorkoutTemplateDetail> DeleteTemplateCommand { get; }
	public Command AddPlanCommand { get; }
	public Command<WorkoutPlanItem> TogglePlanCompletionCommand { get; }
	public Command<WorkoutPlanItem> DeletePlanCommand { get; }
	public Command RefreshGraphCommand { get; }
	public Command SaveGraphPointEditsCommand { get; }
	public Command CancelGraphPointEditsCommand { get; }
	public Command PreviousMonthCommand { get; }
	public Command NextMonthCommand { get; }
	public Command<WorkoutCalendarDay> SelectCalendarDayCommand { get; }
	public Command RefreshCommand { get; }

	public WorkoutSelectionOption? SelectedWorkoutOption
	{
		get => _selectedWorkoutOption;
		set
		{
			if (_selectedWorkoutOption == value)
			{
				return;
			}

			_selectedWorkoutOption = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(IsOtherWorkoutSelected));
			OnPropertyChanged(nameof(UsesTemplateWorkoutSelection));
			PopulateActiveTemplateExerciseLogs();
		}
	}

	public WorkoutTemplateDetail? SelectedProgressTemplate
	{
		get => _selectedProgressTemplate;
		set
		{
			if (_selectedProgressTemplate == value)
			{
				return;
			}

			_selectedProgressTemplate = value;
			OnPropertyChanged();
			_ = UpdateTemplateProgressChartAsync();
		}
	}

	public WorkoutTemplateDetail? SelectedPlanTemplate
	{
		get => _selectedPlanTemplate;
		set
		{
			if (_selectedPlanTemplate == value)
			{
				return;
			}

			_selectedPlanTemplate = value;
			OnPropertyChanged();
		}
	}

	public HtmlWebViewSource TemplateProgressChartSource
	{
		get => _templateProgressChartSource;
		private set
		{
			_templateProgressChartSource = value;
			OnPropertyChanged();
		}
	}

	public bool IsOtherWorkoutSelected => SelectedWorkoutOption?.IsOther == true;
	public bool UsesTemplateWorkoutSelection => SelectedWorkoutOption is { IsOther: false };

	public string OtherWorkoutName
	{
		get => _otherWorkoutName;
		set
		{
			if (_otherWorkoutName == value)
			{
				return;
			}

			_otherWorkoutName = value;
			OnPropertyChanged();
		}
	}

	public string NewWorkoutMinutes
	{
		get => _newWorkoutMinutes;
		set
		{
			if (_newWorkoutMinutes == value)
			{
				return;
			}

			_newWorkoutMinutes = value;
			OnPropertyChanged();
		}
	}

	public DateTime SelectedCompletedOn
	{
		get => _selectedCompletedOn;
		set
		{
			if (_selectedCompletedOn == value)
			{
				return;
			}

			_selectedCompletedOn = value;
			OnPropertyChanged();
		}
	}

	public DateTime SelectedPlanDate
	{
		get => _selectedPlanDate;
		set
		{
			if (_selectedPlanDate == value)
			{
				return;
			}

			_selectedPlanDate = value;
			OnPropertyChanged();
			_ = LoadPlansForSelectedDateAsync();
			BuildCalendarDays();
		}
	}

	public DateTime SelectedTemplateQuickAddDate
	{
		get => _selectedTemplateQuickAddDate;
		set
		{
			if (_selectedTemplateQuickAddDate == value)
			{
				return;
			}

			_selectedTemplateQuickAddDate = value;
			OnPropertyChanged();
		}
	}

	public string DisplayMonthLabel => _displayMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
	public bool IsGraphPointEditorVisible => _editingGraphDay.HasValue;
	public string GraphPointEditorTitle => _editingGraphDay.HasValue
		? $"Edit Workout ({_editingGraphDay.Value:yyyy-MM-dd})"
		: "Edit Workout";

	public string TemplateName
	{
		get => _templateName;
		set
		{
			if (_templateName == value)
			{
				return;
			}

			_templateName = value;
			OnPropertyChanged();
		}
	}

	public int WorkoutCount => Workouts.Count;
	public int TotalMinutes => Workouts.Sum(w => w.DurationMinutes);
	public int CurrentStreakDays => CalculateCurrentStreak();
	public int TemplateCount => WorkoutTemplates.Count;

	public Task RefreshAsync() => LoadAsync();

	private async Task LoadAsync()
	{
		await _loadSemaphore.WaitAsync();
		try
		{
			Workouts.Clear();
			var workouts = await _repository.GetWorkoutsAsync();
			foreach (var workout in workouts.OrderByDescending(w => w.CompletedOn))
			{
				Workouts.Add(workout);
			}

			OnPropertyChanged(nameof(WorkoutCount));
			OnPropertyChanged(nameof(TotalMinutes));
			OnPropertyChanged(nameof(CurrentStreakDays));

			var previousProgressTemplateId = SelectedProgressTemplate?.Id;
			var previousPlanTemplateId = SelectedPlanTemplate?.Id;

			WorkoutTemplates.Clear();
			var templates = await _repository.GetWorkoutTemplatesAsync();
			foreach (var template in templates)
			{
				WorkoutTemplates.Add(template);
			}
			RebuildAddWorkoutOptions(templates);

			SelectedProgressTemplate = WorkoutTemplates.FirstOrDefault(t => t.Id == previousProgressTemplateId) ?? WorkoutTemplates.FirstOrDefault();
			SelectedPlanTemplate = WorkoutTemplates.FirstOrDefault(t => t.Id == previousPlanTemplateId) ?? WorkoutTemplates.FirstOrDefault();
			await LoadPlansForSelectedDateAsync();
			await UpdateTemplateProgressChartAsync();
			BuildCalendarDays();

			OnPropertyChanged(nameof(TemplateCount));
		}
		finally
		{
			_loadSemaphore.Release();
		}
	}

	private async Task AddWorkoutAsync()
	{
		if (SelectedWorkoutOption is null)
		{
			return;
		}

		var minutes = int.TryParse(NewWorkoutMinutes, out var parsedMinutes) && parsedMinutes > 0
			? parsedMinutes
			: 0;

		if (SelectedWorkoutOption.IsOther)
		{
			var otherName = OtherWorkoutName.Trim();
			if (string.IsNullOrWhiteSpace(otherName))
			{
				return;
			}

			await _repository.AddWorkoutAsync(otherName, minutes, SelectedCompletedOn);
			OtherWorkoutName = string.Empty;
		}
		else
		{
			var performedExercises = ActiveTemplateExerciseLogs
				.Select(ParseExerciseLog)
				.Where(input => input is not null)
				.Cast<WorkoutExerciseLogInput>()
				.ToList();
			if (performedExercises.Count == 0)
			{
				return;
			}

			await _repository.AddWorkoutFromTemplateAsync(SelectedWorkoutOption.TemplateId!, minutes, SelectedCompletedOn, performedExercises);
		}

		NewWorkoutMinutes = string.Empty;
		SelectedCompletedOn = DateTime.Today;
		await LoadAsync();
	}

	private async Task DeleteWorkoutAsync(WorkoutEntry? workout)
	{
		if (workout is null)
		{
			return;
		}

		await _repository.DeleteWorkoutAsync(workout.Id);
		await LoadAsync();
	}

	private async Task AddPlanAsync()
	{
		if (_isAddingPlan)
		{
			return;
		}

		if (SelectedPlanTemplate is null)
		{
			return;
		}

		_isAddingPlan = true;
		try
		{
			await _repository.AddWorkoutPlanAsync(SelectedPlanTemplate.Id, SelectedPlanDate);
			await LoadPlansForSelectedDateAsync();
		}
		finally
		{
			_isAddingPlan = false;
		}
	}

	private async Task TogglePlanCompletionAsync(WorkoutPlanItem? plan)
	{
		if (plan is null)
		{
			return;
		}

		await _repository.SetWorkoutPlanCompletedAsync(plan.Id, !plan.IsCompleted);
		await LoadAsync();
	}

	private async Task DeletePlanAsync(WorkoutPlanItem? plan)
	{
		if (plan is null)
		{
			return;
		}

		await _repository.DeleteWorkoutPlanAsync(plan.Id);
		await LoadAsync();
	}

	private async Task LoadPlansForSelectedDateAsync()
	{
		await _planLoadSemaphore.WaitAsync();
		try
		{
			var selectedDate = SelectedPlanDate.Date;
			var plans = await _repository.GetWorkoutPlansAsync(selectedDate, selectedDate);

			var deduped = plans
				.GroupBy(p => $"{p.TemplateId}:{p.PlannedDate:yyyy-MM-dd}")
				.Select(g => g.First())
				.OrderBy(p => p.TemplateName)
				.ToList();

			WorkoutPlansForSelectedDate.Clear();
			foreach (var plan in deduped)
			{
				WorkoutPlansForSelectedDate.Add(plan);
			}
		}
		finally
		{
			_planLoadSemaphore.Release();
		}
	}

	public async Task OpenGraphPointEditorAsync(DateTime day)
	{
		if (SelectedProgressTemplate is null)
		{
			return;
		}

		var rows = await _repository.GetTemplateSessionExercisesAsync(SelectedProgressTemplate.Id, 365);
		var dayRows = rows
			.Where(r => r.CompletedOn.Date == day.Date)
			.OrderBy(r => r.ExerciseName)
			.ToList();
		if (dayRows.Count == 0)
		{
			return;
		}

		GraphPointExerciseEdits.Clear();
		foreach (var row in dayRows)
		{
			GraphPointExerciseEdits.Add(new WorkoutExerciseLogDraft
			{
				SessionExerciseId = row.Id,
				WorkoutId = row.WorkoutId,
				TemplateExerciseId = row.TemplateExerciseId,
				Name = row.ExerciseName,
				Sets = row.Sets.ToString(CultureInfo.InvariantCulture),
				Reps = row.Reps.ToString(CultureInfo.InvariantCulture),
				Weight = row.Weight.ToString("0.##", CultureInfo.InvariantCulture),
				WeightUnit = row.WeightUnit is "kg" ? "kg" : "lb",
				SetColorHex = GetSetColor(row.Sets)
			});
		}

		_editingGraphDay = day.Date;
		OnPropertyChanged(nameof(IsGraphPointEditorVisible));
		OnPropertyChanged(nameof(GraphPointEditorTitle));
	}

	private async Task SaveGraphPointEditsAsync()
	{
		if (_editingGraphDay is null || GraphPointExerciseEdits.Count == 0)
		{
			return;
		}

		var parsedRows = GraphPointExerciseEdits
			.Select(ParseExerciseLog)
			.ToList();
		if (parsedRows.Any(r => r is null))
		{
			return;
		}

		for (var i = 0; i < GraphPointExerciseEdits.Count; i++)
		{
			var editRow = GraphPointExerciseEdits[i];
			var parsed = parsedRows[i]!;
			if (string.IsNullOrWhiteSpace(editRow.SessionExerciseId))
			{
				continue;
			}

			await _repository.UpdateWorkoutSessionExerciseAsync(
				editRow.SessionExerciseId,
				parsed.Sets,
				parsed.Reps,
				parsed.Weight,
				parsed.WeightUnit);
		}

		CancelGraphPointEdits();
		await LoadAsync();
	}

	private void CancelGraphPointEdits()
	{
		_editingGraphDay = null;
		GraphPointExerciseEdits.Clear();
		OnPropertyChanged(nameof(IsGraphPointEditorVisible));
		OnPropertyChanged(nameof(GraphPointEditorTitle));
	}

	private async Task MoveMonthAsync(int monthDelta)
	{
		_displayMonth = _displayMonth.AddMonths(monthDelta);
		if (SelectedPlanDate.Year != _displayMonth.Year || SelectedPlanDate.Month != _displayMonth.Month)
		{
			SelectedPlanDate = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);
		}

		BuildCalendarDays();
		OnPropertyChanged(nameof(DisplayMonthLabel));
		await LoadPlansForSelectedDateAsync();
	}

	private async Task SelectCalendarDayAsync(WorkoutCalendarDay? day)
	{
		if (day is null)
		{
			return;
		}

		SelectedPlanDate = day.Date;
		if (day.Date.Year != _displayMonth.Year || day.Date.Month != _displayMonth.Month)
		{
			_displayMonth = new DateTime(day.Date.Year, day.Date.Month, 1);
			OnPropertyChanged(nameof(DisplayMonthLabel));
		}

		BuildCalendarDays();
		await LoadPlansForSelectedDateAsync();
	}

	private void AddTemplateExerciseRow()
	{
		TemplateExerciseRows.Add(new WorkoutTemplateExerciseDraft());
	}

	private void RemoveTemplateExerciseRow(WorkoutTemplateExerciseDraft? exercise)
	{
		if (exercise is null)
		{
			return;
		}

		TemplateExerciseRows.Remove(exercise);
		if (TemplateExerciseRows.Count == 0)
		{
			AddTemplateExerciseRow();
		}
	}

	private async Task SaveTemplateAsync()
	{
		if (string.IsNullOrWhiteSpace(TemplateName))
		{
			return;
		}

		var exercisesToSave = TemplateExerciseRows
			.Where(row => !string.IsNullOrWhiteSpace(row.Name))
			.Select(row => new WorkoutTemplateExerciseInput
			{
				Name = row.Name.Trim(),
				Sets = int.TryParse(row.Sets, out var sets) ? sets : 0,
				Reps = int.TryParse(row.Reps, out var reps) ? reps : 0,
				Weight = double.TryParse(row.Weight, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight)
					? weight
					: (double.TryParse(row.Weight, NumberStyles.Float, CultureInfo.CurrentCulture, out var localWeight) ? localWeight : 0d),
				WeightUnit = row.WeightUnit is "kg" ? "kg" : "lb"
			})
			.Where(input => input.Sets > 0 && input.Reps > 0 && input.Weight >= 0)
			.ToList();

		if (exercisesToSave.Count == 0)
		{
			return;
		}

		await _repository.AddWorkoutTemplateAsync(TemplateName, exercisesToSave);
		TemplateName = string.Empty;
		TemplateExerciseRows.Clear();
		AddTemplateExerciseRow();
		await LoadAsync();
	}

	private void UseTemplate(WorkoutTemplateDetail? template)
	{
		if (template is null)
		{
			return;
		}

		SelectedWorkoutOption = AddWorkoutOptions.FirstOrDefault(option =>
			!option.IsOther &&
			string.Equals(option.TemplateId, template.Id, StringComparison.Ordinal));
		SelectedProgressTemplate = WorkoutTemplates.FirstOrDefault(t => t.Id == template.Id);
		SelectedPlanTemplate = WorkoutTemplates.FirstOrDefault(t => t.Id == template.Id);
	}

	private async Task QuickAddTemplateWorkoutAsync(WorkoutTemplateDetail? template)
	{
		if (template is null)
		{
			return;
		}

		var performedExercises = template.Exercises
			.Select(e => new WorkoutExerciseLogInput
			{
				TemplateExerciseId = e.Id,
				ExerciseName = e.Name,
				Sets = e.Sets,
				Reps = e.Reps,
				Weight = e.Weight,
				WeightUnit = e.WeightUnit is "kg" ? "kg" : "lb"
			})
			.Where(e => e.Sets > 0 && e.Reps > 0 && e.Weight >= 0)
			.ToList();
		if (performedExercises.Count == 0)
		{
			return;
		}

		await _repository.AddWorkoutFromTemplateAsync(template.Id, 0, SelectedTemplateQuickAddDate.Date, performedExercises);
		await LoadAsync();
	}

	private async Task DeleteTemplateAsync(WorkoutTemplateDetail? template)
	{
		if (template is null)
		{
			return;
		}

		await _repository.DeleteWorkoutTemplateAsync(template.Id);
		await LoadAsync();
	}

	private void PopulateActiveTemplateExerciseLogs()
	{
		ActiveTemplateExerciseLogs.Clear();
		if (SelectedWorkoutOption is not { IsOther: false, TemplateId: not null })
		{
			return;
		}

		var template = WorkoutTemplates.FirstOrDefault(t => t.Id == SelectedWorkoutOption.TemplateId);
		if (template is null)
		{
			return;
		}

		foreach (var exercise in template.Exercises.OrderBy(e => e.Name))
		{
			ActiveTemplateExerciseLogs.Add(new WorkoutExerciseLogDraft
			{
				TemplateExerciseId = exercise.Id,
				Name = exercise.Name,
				Sets = exercise.Sets.ToString(CultureInfo.InvariantCulture),
				Reps = exercise.Reps.ToString(CultureInfo.InvariantCulture),
				Weight = exercise.Weight.ToString("0.##", CultureInfo.InvariantCulture),
				WeightUnit = "lb",
				SetColorHex = GetSetColor(exercise.Sets)
			});
		}
	}

	private async Task UpdateTemplateProgressChartAsync()
	{
		if (SelectedProgressTemplate is null)
		{
			TemplateProgressChartSource = new HtmlWebViewSource { Html = "<html><body style='font-family:sans-serif'>Select a template to view progress.</body></html>" };
			return;
		}

		var sessionRows = await _repository.GetTemplateSessionExercisesAsync(SelectedProgressTemplate.Id, 60);
		var points = sessionRows
			.GroupBy(e => e.CompletedOn.Date)
			.Select(group => new
			{
				Day = group.Key,
				TotalWeight = group.Sum(e => e.Weight),
				MaxSets = group.Max(e => e.Sets),
				Summary = string.Join("; ", group
					.OrderBy(e => e.ExerciseName)
					.Select(e => $"{e.ExerciseName} {e.Sets}x{e.Reps} @ {e.Weight:0.##} {e.WeightUnit}"))
			})
			.OrderBy(p => p.Day)
			.ToList();

		if (points.Count == 0)
		{
			TemplateProgressChartSource = new HtmlWebViewSource
			{
				Html = "<html><body style='font-family:sans-serif'>No workout history for this template yet.</body></html>"
			};
			return;
		}

		var maxY = Math.Max(1d, points.Max(p => p.TotalWeight));
		var width = 760d;
		var height = 300d;
		var paddingLeft = 64d;
		var paddingRight = 20d;
		var paddingTop = 20d;
		var paddingBottom = 68d;
		var innerWidth = width - paddingLeft - paddingRight;
		var innerHeight = height - paddingTop - paddingBottom;
		var step = points.Count == 1 ? 0 : innerWidth / (points.Count - 1);

		var polylinePoints = new StringBuilder();
		var circles = new StringBuilder();
		var xLabels = new StringBuilder();
		var yGridAndLabels = new StringBuilder();

		const int yTickCount = 5;
		for (var tick = 0; tick <= yTickCount; tick++)
		{
			var ratio = tick / (double)yTickCount;
			var y = paddingTop + (innerHeight - (innerHeight * ratio));
			var value = maxY * ratio;
			yGridAndLabels.AppendFormat(
				CultureInfo.InvariantCulture,
				"<line x1='{0:0.##}' y1='{1:0.##}' x2='{2:0.##}' y2='{1:0.##}' stroke='#ececec' stroke-width='1' />",
				paddingLeft,
				y,
				width - paddingRight);
			yGridAndLabels.AppendFormat(
				CultureInfo.InvariantCulture,
				"<text x='{0:0.##}' y='{1:0.##}' text-anchor='end' dominant-baseline='middle' font-size='10' fill='#666'>{2:0.##}</text>",
				paddingLeft - 6,
				y,
				value);
		}

		for (var i = 0; i < points.Count; i++)
		{
			var point = points[i];
			var x = paddingLeft + (i * step);
			var y = paddingTop + (innerHeight - ((point.TotalWeight / maxY) * innerHeight));
			polylinePoints.AppendFormat(CultureInfo.InvariantCulture, "{0:0.##},{1:0.##} ", x, y);
			xLabels.AppendFormat(
				CultureInfo.InvariantCulture,
				"<text x='{0:0.##}' y='{1:0.##}' text-anchor='middle' font-size='9' fill='#555'>{2}</text>",
				x,
				height - 34,
				point.Day.ToString("MM/dd", CultureInfo.InvariantCulture));

			var tooltip = SecurityElement.Escape(
				$"Completed: {point.Day:yyyy-MM-dd}\nCombined Weight: {point.TotalWeight:0.##}\n{point.Summary}") ?? string.Empty;
			var navigateUrl = $"trifold://edit-point/{point.Day:yyyy-MM-dd}";
			circles.AppendFormat(
				CultureInfo.InvariantCulture,
				"<circle cx='{0:0.##}' cy='{1:0.##}' r='5' fill='{2}' ondblclick=\"window.location.href='{4}'\"><title>{3}</title></circle>",
				x,
				y,
				GetSetColor(point.MaxSets),
				tooltip,
				navigateUrl);
		}

		var html =
			"<html><head><meta name='viewport' content='width=device-width, initial-scale=1.0' /></head><body style='margin:0;background:transparent'>" +
			$"<svg viewBox='0 0 {width:0} {height:0}' width='100%' height='240'>" +
			$"<rect x='0' y='0' width='{width:0}' height='{height:0}' fill='transparent' />" +
			yGridAndLabels +
			$"<line x1='{paddingLeft:0.##}' y1='{height - paddingBottom:0.##}' x2='{width - paddingRight:0.##}' y2='{height - paddingBottom:0.##}' stroke='#c8c8c8' stroke-width='1' />" +
			$"<line x1='{paddingLeft:0.##}' y1='{paddingTop:0.##}' x2='{paddingLeft:0.##}' y2='{height - paddingBottom:0.##}' stroke='#c8c8c8' stroke-width='1' />" +
			xLabels +
			$"<text x='{(paddingLeft + ((width - paddingRight - paddingLeft) / 2d)):0.##}' y='{height - 10:0.##}' text-anchor='middle' font-size='11' fill='#555'>Date Completed</text>" +
			$"<text x='16' y='{(paddingTop + (innerHeight / 2d)):0.##}' text-anchor='middle' font-size='11' fill='#555' transform='rotate(-90,16,{(paddingTop + (innerHeight / 2d)):0.##})'>Combined Total Weight (fixed)</text>" +
			$"<polyline points='{polylinePoints}' fill='none' stroke='#4b7bec' stroke-width='2' />" +
			circles +
			"</svg></body></html>";

		TemplateProgressChartSource = new HtmlWebViewSource { Html = html };
	}

	private static WorkoutExerciseLogInput? ParseExerciseLog(WorkoutExerciseLogDraft draft)
	{
		if (!int.TryParse(draft.Sets, out var sets) || sets <= 0)
		{
			return null;
		}

		if (!int.TryParse(draft.Reps, out var reps) || reps <= 0)
		{
			return null;
		}

		var parsedWeight = double.TryParse(draft.Weight, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariantWeight)
			? invariantWeight
			: (double.TryParse(draft.Weight, NumberStyles.Float, CultureInfo.CurrentCulture, out var localWeight) ? localWeight : -1d);
		if (parsedWeight < 0d)
		{
			return null;
		}

		return new WorkoutExerciseLogInput
		{
			TemplateExerciseId = draft.TemplateExerciseId,
			ExerciseName = draft.Name.Trim(),
			Sets = sets,
			Reps = reps,
			Weight = parsedWeight,
			WeightUnit = draft.WeightUnit is "kg" ? "kg" : "lb"
		};
	}

	private int CalculateCurrentStreak()
	{
		var workoutDays = Workouts
			.Select(w => w.CompletedOn.Date)
			.Distinct()
			.ToHashSet();

		var streak = 0;
		var cursor = DateTime.Today;
		while (workoutDays.Contains(cursor))
		{
			streak++;
			cursor = cursor.AddDays(-1);
		}

		return streak;
	}

	private static string GetSetColor(int sets)
	{
		return sets switch
		{
			<= 1 => "#8F00FF", // Violet
			2 => "#4B0082", // Indigo
			3 => "#0000FF", // Blue
			4 => "#00AA44", // Green
			5 => "#FFD000", // Yellow
			6 => "#FF7F00", // Orange
			_ => "#D62828" // Red
		};
	}

	private void BuildCalendarDays()
	{
		CalendarDays.Clear();
		var monthStart = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);
		var firstCellDate = monthStart.AddDays(-(int)monthStart.DayOfWeek);

		for (var i = 0; i < 42; i++)
		{
			var date = firstCellDate.AddDays(i);
			CalendarDays.Add(new WorkoutCalendarDay
			{
				Date = date,
				IsCurrentMonth = date.Month == _displayMonth.Month && date.Year == _displayMonth.Year,
				IsSelected = date.Date == SelectedPlanDate.Date
			});
		}
	}

	private void RebuildAddWorkoutOptions(IReadOnlyList<WorkoutTemplateDetail> templates)
	{
		var previousTemplateId = SelectedWorkoutOption?.TemplateId;
		var previousWasOther = SelectedWorkoutOption?.IsOther == true;

		AddWorkoutOptions.Clear();
		foreach (var template in templates.OrderBy(t => t.Name))
		{
			AddWorkoutOptions.Add(new WorkoutSelectionOption
			{
				TemplateId = template.Id,
				Name = template.Name,
				IsOther = false
			});
		}

		AddWorkoutOptions.Add(new WorkoutSelectionOption
		{
			TemplateId = null,
			Name = "Other Workout",
			IsOther = true
		});

		SelectedWorkoutOption = AddWorkoutOptions.FirstOrDefault(option =>
			option.IsOther == previousWasOther &&
			string.Equals(option.TemplateId, previousTemplateId, StringComparison.Ordinal));

		SelectedWorkoutOption ??= AddWorkoutOptions.FirstOrDefault();
	}
}
