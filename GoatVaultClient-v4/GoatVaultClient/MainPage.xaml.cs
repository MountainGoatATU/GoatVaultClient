using GoatVaultClient.Services;
using GoatVaultClient.ViewModels;
using Microsoft.Maui.Controls;

namespace GoatVaultClient
{
    public partial class MainPage : ContentPage
    {
        private GoatTipsService _goatTipsService;

        public MainPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            _goatTipsService = viewModel.GoatTipsService;

            viewModel.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(viewModel.IsGoatCommentVisible))
                {
                    if (viewModel.IsGoatCommentVisible)
                    {
                        GoatBubbleStack.Opacity = 1;
                        GoatMascot.Opacity = 1;
                    }
                    else
                    {
                        GoatBubbleStack.Opacity = 0;
                        GoatMascot.Opacity = 0;
                    }
                }
            };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Safely cast the BindingContext and call the method
            if (BindingContext is MainPageViewModel vm)
            {
                vm.LoadVaultData();
                vm.StartRandomGoatComments();
            }
        }

        private void OnCategoriesCollectionMenuOpening(object sender, EventArgs e)
        {
            var flyout = sender as MenuFlyout;
            var item = flyout?.BindingContext;

            if (item == null)
                return;

            // Specifically update the Active List
            CategoriesCollection.SelectedItem = item;

            // Optional: Clear selection in the other list if you want exclusive selection
            CategoriesCollection.SelectedItem = null;
        }

        private void OnEntriesCollectionMenuOpening(object sender, EventArgs e)
        {
            var flyout = sender as MenuFlyout;
            var item = flyout?.BindingContext;

            if (item == null)
                return;

            // Specifically update the Active List
            EntriesCollection.SelectedItem = item;

            // Optional: Clear selection in the other list if you want exclusive selection
            EntriesCollection.SelectedItem = null;
        }
    }
}
