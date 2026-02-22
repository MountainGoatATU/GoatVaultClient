using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class EntryDetailPage : ContentPage
{
	public EntryDetailPage(EntryDetailsViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
	}
}