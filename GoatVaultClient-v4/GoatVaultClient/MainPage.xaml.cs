using GoatVaultClient.Services;
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

            void ApplyVisibility()
            {
                var visible = viewModel.IsGoatCommentVisible;
                GoatBubbleStack.Opacity = visible ? 1 : 0;
                GoatMascot.Opacity = visible ? 1 : 0;
            }

            // Start hidden until first tip shows
            ApplyVisibility();

            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainPageViewModel.IsGoatCommentVisible))
                {
                    ApplyVisibility();
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
