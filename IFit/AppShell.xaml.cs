using IFit.Views;

namespace IFit
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Routing
            Routing.RegisterRoute("MainPage", typeof(MainPage));
            Routing.RegisterRoute("SignUpPage", typeof(SignUpPage));
        }
    }
}
