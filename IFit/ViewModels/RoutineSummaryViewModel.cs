using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Helper;
using IFit.Models.Dtos.AI;
using IFit.Services;

namespace IFit.ViewModels
{
    [QueryProperty(nameof(Routine), "Routine")]
    public partial class RoutineSummaryViewModel : ObservableObject
    {
        #region Constants

        // Número estimado de ejercicios que caben en la tarjeta sin necesitar scroll
        // (basado en pantalla ~800dp, overhead de cabecera/pie ~150dp, ~66dp por ejercicio ≈ 6 caben)
        private const int ScrollThreshold = 5;

        #endregion

        #region Services

        private readonly TrainingService _trainingService;

        #endregion

        #region Properties

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasMultipleDays))]
        [NotifyPropertyChangedFor(nameof(DayIndicatorText))]
        [NotifyPropertyChangedFor(nameof(CurrentDay))]
        [NotifyPropertyChangedFor(nameof(ShowScrollIndicator))]
        private RoutineResponseDto? _routine;

        [ObservableProperty]
        private string _messageAI = string.Empty;

        [ObservableProperty]
        private bool _isSaving = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentDay))]
        [NotifyPropertyChangedFor(nameof(DayIndicatorText))]
        [NotifyPropertyChangedFor(nameof(ShowScrollIndicator))]
        private int _currentDayIndex = 0;

        /// <summary>Día de entrenamiento actualmente visible.</summary>
        public TrainingDayDto? CurrentDay =>
            Routine?.Days?.ElementAtOrDefault(CurrentDayIndex);

        /// <summary>Texto indicador de posición: "Día 2 de 5".</summary>
        public string DayIndicatorText =>
            Routine?.Days?.Count > 0
                ? $"Día {CurrentDayIndex + 1} de {Routine.Days.Count}"
                : string.Empty;

        /// <summary>True si la rutina tiene más de un día (muestra botones nav).</summary>
        public bool HasMultipleDays => (Routine?.Days?.Count ?? 0) > 1;

        /// <summary>
        /// True si el día actual tiene más ejercicios de los que caben sin scroll.
        /// Sirve para mostrar el indicador visual dentro de la tarjeta.
        /// </summary>
        public bool ShowScrollIndicator =>
            (CurrentDay?.Exercises?.Count ?? 0) > ScrollThreshold;


        [ObservableProperty]
        public partial Boolean IsLoading { get; set; } = false;

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;
        
        #endregion

        #region Constructor

        public RoutineSummaryViewModel(TrainingService trainingService)
        {
            _trainingService = trainingService;
        }

        public RoutineSummaryViewModel() : this(
            App.GetService<TrainingService>() ?? throw new InvalidOperationException("TrainingService no registrado"))
        {
        }

        #endregion

        #region Property Hooks

        partial void OnRoutineChanged(RoutineResponseDto? value)
        {
            if (value != null)
            {
                MessageAI = value.Description;
                CurrentDayIndex = 0;
            }
        }

        #endregion

        #region Commands

        /// <summary>Retrocede al día anterior de forma circular.</summary>
        [RelayCommand]
        private void GoToPreviousDay()
        {
            var count = Routine?.Days?.Count ?? 0;
            if (count == 0) return;
            CurrentDayIndex = CurrentDayIndex == 0 ? count - 1 : CurrentDayIndex - 1;
        }

        /// <summary>Avanza al día siguiente de forma circular.</summary>
        [RelayCommand]
        private void GoToNextDay()
        {
            var count = Routine?.Days?.Count ?? 0;
            if (count == 0) return;
            CurrentDayIndex = (CurrentDayIndex + 1) % count;
        }

        /// <summary>Vuelve al resumen del cuestionario para regenerar la rutina.</summary>
        [RelayCommand]
        private async Task TryAgainAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(
                    $"Error al navegar: {ex.Message}");
            }
        }

        /// <summary>Guarda la rutina generada en el perfil del usuario.</summary>
        [RelayCommand]
        private async Task SaveRoutineAsync()
        {
            IsLoading = true;
            StatusMessage = "Guardando tu rutina...";

            if (Routine == null)
            {
                await NotificationService.ShowErrorAsync("No hay ninguna rutina para guardar.");
                return;
            }

            try
            {
                IsSaving = true;
                long userId = Preferences.Get("UserId", 0L);

                var response = await _trainingService.createRoutineAsync(userId, Routine);

                if (response == null)
                {
                    await NotificationService.ShowErrorAsync(
                        "No se pudo guardar tu rutina. Por favor, intenta nuevamente.");
                    return;
                }

                IsLoading = false;
                StatusMessage = string.Empty;
                await NotificationService.ShowSuccessAsync("¡Tu rutina ha sido guardada correctamente!");
                
                IsLoading = true;
                StatusMessage = "Cargando tu rutina guardada...";
                await Shell.Current.GoToAsync("///HomeView", false);

                IsLoading = false;
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync($"Error al guardar la rutina: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        #endregion
    }
}
