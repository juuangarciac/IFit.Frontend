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
            Routing.RegisterRoute("SignUpView", typeof(SignUpView));
            Routing.RegisterRoute("SignInView", typeof(SignInView));
            Routing.RegisterRoute("VerificationView", typeof(VerificationView));
            Routing.RegisterRoute("HomeView", typeof(HomeView));
            Routing.RegisterRoute("GetStartedView", typeof(GetStartedView));
            Routing.RegisterRoute("CoachModelTypeSelectionView", typeof(CoachModelTypeSelectionView));
        }
    }
}
