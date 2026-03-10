namespace IFit.Views.Templates;

public partial class HomeFooterView : ContentView
{
    public HomeFooterView()
    {
        InitializeComponent();
    }

    // Navega a la pantalla principal (Hoy)
    private async void OnHomeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//HomePage");
    }

    // Navega a la pantalla de Plan de entrenamiento
    private async void OnPlanClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//PlanPage");
    }

    // Navega a la pantalla de Actividades / Workouts
    private async void OnWorkoutsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//WorkoutsPage");
    }

    // Navega a la pantalla de Comunidad
    private async void OnCommunityClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//CommunityPage");
    }

    // Navega a la pantalla de Ayuda
    private async void OnHelpClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//HelpPage");
    }
}