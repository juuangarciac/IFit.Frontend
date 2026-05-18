using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models;
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
            "ai_coach_ifit.png",
            "Tu entrenador personal de IA",
            "Pregunta a tu entrenador personal de IA cualquier duda que tengas sobre tu entrenamiento, nutrición o recuperación.",
            "Entendido",
            "Mejor después"
        ),
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
    public partial Boolean IsLoading { get; set; } = false;

    [ObservableProperty]
    public partial String StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial String ButtonContent { get; set; } = "Pregunta a tu coach";


    [ObservableProperty]
    public partial Boolean DoesntHaveRoutine { get; set; } = false;

    [ObservableProperty]
    public partial int CurrentCarouselPosition { get; set; } = 0;

    #endregion

    #region Services
    private AppUserService _appUserService;
    private TrainingService _trainingService;

    #endregion

    private bool _isInitialized = false;
    private long _lastUserId = 0;

    #region Constructor

    public HomeViewModel(TrainingService trainingService,
        AppUserService appUserService) {
        _trainingService = trainingService;
        _appUserService = appUserService;
    }

    public HomeViewModel() : this(
        App.GetService<TrainingService>() ?? throw new InvalidOperationException("TrainingService no registrado"),
        App.GetService<AppUserService>() ?? throw new InvalidOperationException("AppUserService no esta registrado")) {
    }

    private async Task InitializeAsync()
    {
        try
        {
            DoesntHaveRoutine = false;
            StatusMessage = "Cargando tu rutina actual...";

            var userId = Preferences.Get("UserId", 0L);

            // CoachModelTypeName ya está en Preferences desde el login: no hace falta llamar a /users/{id}
            var coachName = Preferences.Get("CoachModelTypeName", "tu coach");
            ButtonContent = "Pregunta a " + coachName;

            var cachedRoutineId = Preferences.Get("CurrentRoutineId", 0L);
            if (cachedRoutineId > 0)
            {
                Routine = await _trainingService.getRoutineByIdAsync(cachedRoutineId);
                if (Routine == null)
                {
                    // Rutina cacheada ya no existe (borrada/desactivada): fallback
                    Preferences.Remove("CurrentRoutineId");
                    Routine = await _trainingService.getLatestActiveRoutineByUserIdAsync(userId);
                    if (Routine?.Id != null)
                        Preferences.Set("CurrentRoutineId", (int)Routine.Id);
                }
            }
            else
            {
                Routine = await _trainingService.getLatestActiveRoutineByUserIdAsync(userId);
                if (Routine?.Id != null)
                    Preferences.Set("CurrentRoutineId", (int)Routine.Id);
            }

            if (Routine == null)
            {
                StatusMessage = "No se ha encontrado la rutina actual.";
                DoesntHaveRoutine = true;
                IsLoading = false;
                return;
            }

            TrainingDayDto = await _trainingService
                .getRoutineDayAsync((long)Routine.Id, (int)Routine.CurrentDay);

            if (TrainingDayDto == null)
            {
                StatusMessage = "No se ha encontrado una sesión activa para hoy.";
                IsLoading = false;
                return;
            }

            Debug.WriteLine("TrainingDayDto: " + TrainingDayDto);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ Error inicializando Home: {ex.Message}");
            StatusMessage = "Error al cargar los datos.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Methods

    [RelayCommand]
    public Task AppearingAsync()
    {
        long currentUserId = Preferences.Get("UserId", 0L);

        // Re-inicializar si es la primera vez o si cambió el usuario activo entre sesiones
        if (_isInitialized && currentUserId == _lastUserId) return Task.CompletedTask;

        _isInitialized = true;
        _lastUserId = currentUserId;
        IsLoading = true;
        _ = InitializeAsync();
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task OpenTrainingDayDetailAsync()
    {
        IsLoading = true;
        var navigationParameter = new Dictionary<String, Object>()
        {
            {"Routine", Routine },
            {"TrainingDay", TrainingDayDto }
        };
        await Shell.Current.GoToAsync("TrainingDayDetailView", navigationParameter);
        IsLoading = false;
    }

    [RelayCommand]
    public async Task GoToChatViewAsync()
    {
        IsLoading = true;
        await Shell.Current.GoToAsync("ChatAIView");
        IsLoading = false;
    }

    [RelayCommand]
    public async Task GoToWeeklySummaryAsync()
    {
        if (Routine == null) return;
        IsLoading = true;
        var navigationParameter = new Dictionary<string, object>()
        {
            { "Routine", Routine }
        };
        await Shell.Current.GoToAsync("WeeklySummaryView", navigationParameter);
        IsLoading = false;
    }

    [RelayCommand]
    public void NextCard()
    {
        if (CurrentCarouselPosition < InformationCarrousel.Count - 1)
            CurrentCarouselPosition++;
        else
            CurrentCarouselPosition = 0;
    }

    [RelayCommand]
    public async Task GoToPlanAsync()
    {
        await Shell.Current.GoToAsync("//PlanSummaryView");
    }

    [RelayCommand]
    public async Task GoToProfileAsync()
    {
        await Shell.Current.GoToAsync("ProfileView");
    }

    #endregion
}
