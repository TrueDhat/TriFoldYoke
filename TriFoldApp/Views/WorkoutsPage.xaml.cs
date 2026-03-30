using System;
using TriFoldApp.ViewModels;

namespace TriFoldApp.Views;

public partial class WorkoutsPage : ContentPage
{
	private readonly WorkoutsViewModel _viewModel;

	public WorkoutsPage(WorkoutsViewModel viewModel)
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

	private async void OnTemplateProgressWebViewNavigating(object? sender, WebNavigatingEventArgs e)
	{
		if (!Uri.TryCreate(e.Url, UriKind.Absolute, out var uri))
		{
			return;
		}

		if (!string.Equals(uri.Scheme, "trifold", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		if (!string.Equals(uri.Host, "edit-point", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		var dayToken = uri.AbsolutePath.Trim('/');
		if (!DateTime.TryParseExact(dayToken, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var day))
		{
			e.Cancel = true;
			return;
		}

		e.Cancel = true;
		await _viewModel.OpenGraphPointEditorAsync(day);
	}
}
