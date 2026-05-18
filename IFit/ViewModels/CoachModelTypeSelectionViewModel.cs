using CommunityToolkit.Mvvm.ComponentModel;
using IFit.Helper;
using IFit.Models;
using IFit.Models.Dtos;
using IFit.Models.Dtos.Coach;
using IFit.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IFit.ViewModels;
public partial class CoachModelTypeSelectionViewModel : ObservableObject
{
    #region Services

    private DatabaseService? databaseService = App.GetService<DatabaseService>();
    private AppUserService? appUserService = App.GetService<AppUserService>();
    private CoachModelTypeService _coachModelTypeService = new CoachModelTypeService();

    #endregion

    #region Properties 
    [ObservableProperty]
    public partial CoachModelTypeResponseDto? SelectedCoachModelType { get; set; }

    partial void OnSelectedCoachModelTypeChanged(CoachModelTypeResponseDto? value)
    {
        _ = HandleOnSelectedCoachChanged(value);
    }

    [ObservableProperty]
    public partial List<CoachModelTypeResponseDto>? CoachModelTypes { get; set; }

    #endregion

    #region Constructor
    public CoachModelTypeSelectionViewModel(
        CoachModelTypeService coachModelTypeService,
        DatabaseService databaseService,
        AppUserService appUserService
        )
	{
        _coachModelTypeService = coachModelTypeService;
        this.databaseService = databaseService;
        this.appUserService = appUserService;
    }

    public CoachModelTypeSelectionViewModel() : this(
         App.GetService<CoachModelTypeService>() ?? throw new InvalidOperationException("CoachModelTypeService no registrado"),
         App.GetService<DatabaseService>() ?? throw new InvalidOperationException("DatabaseService no registrado"),
         App.GetService<AppUserService>() ?? throw new InvalidOperationException("AppUserService no registrado"))
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
         await LoadCoachModelTypes();
    }

    #endregion


    #region Methods
    private async Task LoadCoachModelTypes()
    {
        CoachModelTypes = await _coachModelTypeService.GetCoachModelTypes();

        if (CoachModelTypes == null || !CoachModelTypes.Any())
        {
            if (App.Current?.MainPage != null)
            {
                await NotificationService.ShowErrorAsync("No se encontraron tipos de entrenador. Por favor, inténtelo más tarde.");
            }
            await Shell.Current.GoToAsync("//ErrorView");
            return;
        }
    }

    private async Task HandleOnSelectedCoachChanged(CoachModelTypeResponseDto? selectedCoachModelType)
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

        AppUserResponseDto? response = await appUserService.SetCoachModelType(user.Id, selectedCoachModelType.Id);
        if (response == null
            || string.IsNullOrEmpty(response?.CoachModelTypeName))
        {
            await ErrorHandler.HandleErrorAsync("Failed to set coach model type.", "//ErrorView",
                "Error",
                "No se pudo establecer el tipo de modelo de entrenador. Por favor, int�ntelo m�s tarde.");
            return;
        }
        Preferences.Set("CoachId",            selectedCoachModelType.Id);
        Preferences.Set("CoachName",           selectedCoachModelType.Name);
        Preferences.Set("CoachModelTypeName",  selectedCoachModelType.Name);

        await databaseService.SaveAppUserAsync(response.toEntity());

        await Shell.Current.GoToAsync("AppUserQuestionnaireView");
        SelectedCoachModelType = null;
    }
    #endregion
}