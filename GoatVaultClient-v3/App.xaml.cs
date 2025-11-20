namespace GoatVaultClient_v3
{
    using Microsoft.Extensions.DependencyInjection;

    public partial class App : Application
    {
        private readonly IServiceProvider _services;
        public IServiceProvider Services => _services;

        public App(IServiceProvider services)
        {
            _services = services;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var introductionPage = _services.GetRequiredService<Introduction>();
            return new Window(new NavigationPage(introductionPage));
        }
    }
}
