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
        await Shell.Current.GoToAsync("//HomeView");
    }

    // Navega a la pantalla de Plan de entrenamiento
    private async void OnPlanClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//PlanSummaryView");
    }

    // Navega al catálogo de ejercicios
    private async void OnWorkoutsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ExerciseCatalogView");
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
