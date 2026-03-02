using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class EntryDetailPage
{
	public EntryDetailPage(EntryDetailsViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
	}
}