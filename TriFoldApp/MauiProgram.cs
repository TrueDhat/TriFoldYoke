using Microsoft.Extensions.Logging;
using TriFoldApp.Services;
using TriFoldApp.ViewModels;
using TriFoldApp.Views;

namespace TriFoldApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<ITrackerRepository, SQLiteTrackerRepository>();
		builder.Services.AddTransient<TodayViewModel>();
		builder.Services.AddTransient<MindViewModel>();
		builder.Services.AddTransient<SpiritViewModel>();
		builder.Services.AddTransient<WorkoutsViewModel>();
		builder.Services.AddTransient<TodayPage>();
		builder.Services.AddTransient<MindPage>();
		builder.Services.AddTransient<SpiritPage>();
		builder.Services.AddTransient<WorkoutsPage>();
		builder.Services.AddSingleton<AppShell>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
