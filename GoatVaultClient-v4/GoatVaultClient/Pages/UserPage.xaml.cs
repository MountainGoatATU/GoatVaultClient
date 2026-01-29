using GoatVaultClient_v3.ViewModels;

namespace GoatVaultClient_v3.Pages
{
    public partial class UserPage : ContentPage
    {
        public UserPage(UserPageViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}