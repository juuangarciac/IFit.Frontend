using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos;
using IFit.Models.Dtos.AI;
using IFit.Resources.Items;
using IFit.Services;
using System.Diagnostics;

namespace IFit.ViewModels;

public partial class HomeViewModel : ObservableObject
{

    #region Properties

    private List<IFitCard?> informationCarrousel = new List<IFitCard?>()
    {
        new IFitCard(
            "smart_watch_ifit.png",
            "Conecta tus aplicaciones y relojes",
            "Conecta tus aplicaciones y relojes a IFit para\r\nsacar el máximo partido a tu entrenamiento.",
            "Conectar",
            "Descartar")
    };

    public List<IFitCard?> InformationCarrousel
    {
        get { return informationCarrousel; }
        set
        {
            if(informationCarrousel != value)
            {
                informationCarrousel = value;
                OnPropertyChanged(nameof(InformationCarrousel));
            }
        }
    }

    [ObservableProperty]
    public partial RoutineResponseDto? Routine { get; set; }

    [ObservableProperty]
    public partial TrainingDayDto? TrainingDayDto { get; set; }

    [ObservableProperty]
    public partial Boolean IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial String StatusMessage { get; set; } = string.Empty;

    #endregion

    #region Services
    private TrainingService _trainingService;

    #endregion

    #region Constructor

    public HomeViewModel(TrainingService trainingService) { 
        _trainingService = trainingService;
    }

    public HomeViewModel() : this(
        App.GetService<TrainingService>() ?? throw new InvalidOperationException("TrainingService no registrado")) {
    }


    private async Task InitializeAsync()
    {
        IsLoading = true;

        StatusMessage = "Cargando tu rutina actual...";

        var userId = Preferences.Get("UserId", 0L);

        List<RoutineResponseDto>? allActivesRoutines = await _trainingService
            .getActivesRoutinesByUserIdAsync(userId);

        if(allActivesRoutines == null)
        {
            StatusMessage = "No se ha encontrado la rutina actual.";
            return;
        }

        Routine = allActivesRoutines.FirstOrDefault();

        TrainingDayDto = await _trainingService
            .getRoutineDayAsync( (long)Routine.Id, (int)Routine.CurrentDay);

        if(TrainingDayDto == null)
        {
            StatusMessage = "No se ha encontrado una sesión activa para hoy.";
            return;
        }

        Debug.WriteLine("TrainingDayDto: " + TrainingDayDto);

        IsLoading = false;
        return;
    }

    #endregion

    #region Methods

    [RelayCommand]
    public async Task AppearingAsync()
    {
        await InitializeAsync();
    }

    [RelayCommand]
    public async Task OpenTrainingDayDetailAsync()
    {
        var navigationParameter = new Dictionary<String, Object>()
            {
                {"Routine", Routine },
                {"TrainingDay", TrainingDayDto }
            };

        await Shell.Current.GoToAsync($"//TrainingDayDetailView", navigationParameter);
    }

    #endregion
}