namespace GoatVaultClient_v3
{
    using GoatVaultClient_v3.Services;
    using Microsoft.Extensions.DependencyInjection;

    public partial class App : Application
    {
        private readonly IServiceProvider _services;
        private readonly VaultSessionService _sessionService;
        public IServiceProvider Services => _services;

        public App(IServiceProvider services, VaultSessionService sessionService)
        {
            _services = services;
            InitializeComponent();
            _sessionService = sessionService;
            //MainPage = services.GetRequiredService<MainPage>();
        }

        protected override void OnSleep()
        {
            _sessionService.Lock();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var introductionPage = _services.GetRequiredService<IntroductionPage>();
            return new Window(new NavigationPage(introductionPage));
        }
    }
}
