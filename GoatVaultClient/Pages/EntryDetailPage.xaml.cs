using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class EntryDetailPage
{
	public EntryDetailPage(MainPageViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
	}
}