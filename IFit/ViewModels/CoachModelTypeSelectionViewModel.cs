using IFit.Helper;
using IFit.Models;
using IFit.Models.Dtos;
using IFit.Models.Dtos.Coach;
using IFit.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IFit.ViewModels;
public class CoachModelTypeSelectionViewModel : INotifyPropertyChanged
{
    private DatabaseService? databaseService = App.GetService<DatabaseService>();
    private AppUserService? appUserService = App.GetService<AppUserService>();

    private CoachModelTypeResponseDto? selectedCoachModelType = null;
    public CoachModelTypeResponseDto? SelectedCoachModelType
    {
        get => selectedCoachModelType;
        set
        {
            if (selectedCoachModelType != value)
            {
                selectedCoachModelType = value;

                OnPropertyChanged(nameof(SelectedCoachModelType));
                OnSelectedCoachChanged(selectedCoachModelType);
            }
        }
    }

    private async void OnSelectedCoachChanged(CoachModelTypeResponseDto? selectedCoachModelType)
    {
        if (selectedCoachModelType == null || databaseService == null || appUserService == null)
        {
            await ErrorHandler.HandleErrorAsync("Selected coach model type is null or services are not initialized.", "//ErrorView");
            return;
        }

        selectedCoachModelType = CoachModelTypes?.FirstOrDefault(c => c.Name == selectedCoachModelType.Name);
        if (selectedCoachModelType == null)
        {
            await ErrorHandler.HandleErrorAsync("Selected coach model type not found in the list.", "//ErrorView");
            return;
        }
         
        AppUser? user = await databaseService.GetCurrentUserAsync();
        if (user == null)
        {
            await ErrorHandler.HandleErrorAsync("No user found in the database.", "//ErrorView");
            return;
        }

        AppUser? result = /*await appUserService.SetCoachModelType(user.Id, selectedCoachModelType.Id)*/ null;
        if (result == null)
        {
            await ErrorHandler.HandleErrorAsync("Failed to set coach model type.", "//ErrorView",
                "Error",
                "No se pudo establecer el tipo de modelo de entrenador. Por favor, inténtelo más tarde.");
            return;
        }

        await databaseService.SaveAppUserAsync(result);
        await Shell.Current.GoToAsync("//ExperienceLevelSelectionView");
    }


    private List<CoachModelTypeResponseDto>? coachModelTypes = new List<CoachModelTypeResponseDto>();
    public List<CoachModelTypeResponseDto>? CoachModelTypes
    {
        get => coachModelTypes;
        set
        {
            if (coachModelTypes != value)
            {
                coachModelTypes = value;
                OnPropertyChanged(nameof(CoachModelTypes));
            }
        }
    }

    private CoachModelTypeService _coachModelTypeService = new CoachModelTypeService();

    public CoachModelTypeSelectionViewModel()
	{
		LoadCoachModelTypes();
    }

	public async void LoadCoachModelTypes()
	{
        CoachModelTypes = await _coachModelTypeService.GetCoachModelTypes();
 
        if (CoachModelTypes == null || !CoachModelTypes.Any())
        {
            if(App.Current?.MainPage != null)
            {
                await App.Current.MainPage.DisplayAlert("Error", "No se encontraron tipos de modelos de entrenador. Por favor, inténtelo más tarde.", "OK");
            }
            await Shell.Current.GoToAsync("//ErrorView");
            return;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}