namespace GoatVaultClient_v3
{
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
            return new Window(new NavigationPage(new Introduction(_services)));
        }
    }
}
