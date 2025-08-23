namespace IFit
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; set; }
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

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
