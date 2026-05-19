namespace IFit
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; set; }
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            // La app usa tema oscuro permanente — forzar Light para que AppThemeBinding
            // elija siempre los valores "Light" (#EEEEEE) sobre fondos oscuros.
            UserAppTheme = AppTheme.Light;

            // Register the service provider for dependency injection
            Services = serviceProvider;

            if(Services == null)
            {
                throw new InvalidOperationException("Service provider is not initialized.");
            }

            MainPage = new AppShell();
        }

        public static T? GetService<T>() => ((App)App.Current).Services.GetService<T>() ?? throw new InvalidOperationException("DatabaseService is not initialized.");
    }
}
