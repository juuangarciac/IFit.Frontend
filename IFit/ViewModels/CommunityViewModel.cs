using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IFit.ViewModels;

public partial class CommunityViewModel : ObservableObject
{
    private const string ShareText =
        "¡Estoy entrenando con IFit, mi asistente de fitness con IA! 💪 " +
        "Genera rutinas personalizadas adaptadas a mis objetivos y me acompaña en cada sesión. " +
        "¿Te apuntas? #IFit #FitnessIA #EntrenamientoInteligente";

    [RelayCommand]
    private async Task ShareAsync()
    {
        await Share.RequestAsync(new ShareTextRequest
        {
            Text = ShareText,
            Title = "Comparte IFit"
        });
    }

    [RelayCommand]
    private static async Task GoToHomeAsync()
    {
        await Shell.Current.GoToAsync("//HomeView");
    }
}
