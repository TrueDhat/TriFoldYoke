using TriFoldApp.ViewModels;

namespace TriFoldApp.Views;

public partial class TodayPage : ContentPage
{
	private readonly TodayViewModel _viewModel;

	public TodayPage(TodayViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.RefreshAsync();
	}
}
