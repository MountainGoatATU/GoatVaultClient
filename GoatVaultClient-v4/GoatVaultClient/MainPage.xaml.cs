using GoatVaultClient.ViewModels;
using Microsoft.Maui.Controls;

namespace GoatVaultClient
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Safely cast the BindingContext and call the method
            if (BindingContext is MainPageViewModel vm)
            {
                vm.LoadVaultData();
            }
        }

        private void OnCategoriesCollectionMenuOpening(object sender, EventArgs e)
        {
            var flyout = sender as MenuFlyout;
            var item = flyout?.BindingContext;

            if (item != null)
            {
                // Specifically update the Active List
                CategoriesCollection.SelectedItem = item;

                // Optional: Clear selection in the other list if you want exclusive selection
                CategoriesCollection.SelectedItem = null;
            }
        }

        private void OnEntriesCollectionMenuOpening(object sender, EventArgs e)
        {
            var flyout = sender as MenuFlyout;
            var item = flyout?.BindingContext;

            if (item != null)
            {
                // Specifically update the Active List
                EntriesCollection.SelectedItem = item;

                // Optional: Clear selection in the other list if you want exclusive selection
                EntriesCollection.SelectedItem = null;
            }
        }
    }
}
