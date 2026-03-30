namespace TriFoldApp.Models;

public class WorkoutCalendarDay
{
	public DateTime Date { get; init; }
	public int DayNumber => Date.Day;
	public bool IsCurrentMonth { get; init; }
	public bool IsSelected { get; set; }
}
