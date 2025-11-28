using GoatVaultClient_v3.ViewModels;

namespace GoatVaultClient_v3;

public partial class EducationPage : ContentPage
{
	private readonly EducationPageViewModel _viewModel;
    public EducationPage(EducationPageViewModel vm)
	{
		InitializeComponent();
		_viewModel = vm;
		BindingContext = vm;	
    }

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.LoadDocumentAsync();
    }
}