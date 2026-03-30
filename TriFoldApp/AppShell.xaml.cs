using TriFoldApp.Views;

namespace TriFoldApp;

public partial class AppShell : Shell
{
	public AppShell(TodayPage todayPage, WorkoutsPage workoutsPage, MindPage mindPage, SpiritPage spiritPage)
	{
		InitializeComponent();

		Items.Add(new TabBar
		{
			Items =
			{
				new ShellContent
				{
					Title = "Tri-Fold",
					Icon = "dotnet_bot.png",
					Content = todayPage
				},
				new ShellContent
				{
					Title = "Body",
					Icon = "dotnet_bot.png",
					Content = workoutsPage
				},
				new ShellContent
				{
					Title = "Mind",
					Icon = "dotnet_bot.png",
					Content = mindPage
				},
				new ShellContent
				{
					Title = "Spirit",
					Icon = "dotnet_bot.png",
					Content = spiritPage
				}
			}
		});
	}
}
