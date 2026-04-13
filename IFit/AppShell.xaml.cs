using IFit.Views;
using IFit.Views.Components;

namespace IFit
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Rutas drill-down: no están en AppShell.xaml, se navega relativamente (sin //).
            // Las vistas declaradas como ShellContent en AppShell.xaml usan navegación
            // absoluta (// o ///) y NO deben registrarse aquí (duplicado → ruta ambigua).
            Routing.RegisterRoute("TrainingDayDetailView", typeof(TrainingDayDetailView));
            Routing.RegisterRoute("ChatAIView",            typeof(ChatAIView));
            Routing.RegisterRoute("WeeklySummaryView",     typeof(WeeklySummaryView));
            Routing.RegisterRoute("ProfileView",           typeof(ProfileView));
            Routing.RegisterRoute("ExerciseCatalogView",   typeof(ExerciseCatalogView));
            Routing.RegisterRoute("ExerciseDetailView",    typeof(ExerciseDetailView));
        }
    }
}
