using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class EntryDetailPage : ContentPage
{
	public EntryDetailPage(MainPageViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
	}
}