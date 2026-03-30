using TriFoldApp.ViewModels;

namespace TriFoldApp.Views;

public partial class MindPage : ContentPage
{
	private readonly MindViewModel _viewModel;

	public MindPage(MindViewModel viewModel)
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
