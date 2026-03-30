using TriFoldApp.ViewModels;

namespace TriFoldApp.Views;

public partial class SpiritPage : ContentPage
{
	private readonly SpiritViewModel _viewModel;

	public SpiritPage(SpiritViewModel viewModel)
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
