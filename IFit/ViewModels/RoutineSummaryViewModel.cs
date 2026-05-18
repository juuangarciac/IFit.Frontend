using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Helper;
using IFit.Models.Dtos.AI;
using IFit.Services;
using System.Diagnostics;

namespace IFit.ViewModels
{
    [QueryProperty(nameof(Routine), "Routine")]
    public partial class RoutineSummaryViewModel : ObservableObject
    {
        #region Constants

        private const int ScrollThreshold = 5;

        #endregion

        #region Services

        private readonly TrainingService _trainingService;
        private readonly AppUserService _appUserService;
        private readonly AIRoutineService _aiRoutineService;

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
        public partial bool ShowNoteInput { get; set; } = false;

        [ObservableProperty]
        public partial string UserNote { get; set; } = string.Empty;

        [ObservableProperty]
        public partial Boolean IsLoading { get; set; } = false;

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        #endregion

        #region Constructor

        public RoutineSummaryViewModel(TrainingService trainingService, AppUserService appUserService, AIRoutineService aiRoutineService)
        {
            _trainingService = trainingService;
            _appUserService = appUserService;
            _aiRoutineService = aiRoutineService;
        }

        public RoutineSummaryViewModel() : this(
            App.GetService<TrainingService>() ?? throw new InvalidOperationException("TrainingService no registrado"),
            App.GetService<AppUserService>() ?? throw new InvalidOperationException("AppUserService no registrado"),
            App.GetService<AIRoutineService>() ?? throw new InvalidOperationException("AIRoutineService no registrado"))
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

        /// <summary>Abre el overlay para que el usuario escriba una nota antes de regenerar.</summary>
        [RelayCommand]
        private void TryAgain() => ShowNoteInput = true;

        /// <summary>Cierra el overlay sin regenerar.</summary>
        [RelayCommand]
        private void CancelTryAgain()
        {
            ShowNoteInput = false;
            UserNote = string.Empty;
        }

        /// <summary>Regenera la rutina con la nota opcional del usuario.</summary>
        [RelayCommand]
        private async Task ConfirmTryAgainAsync()
        {
            ShowNoteInput = false;
            IsLoading = true;
            StatusMessage = "Regenerando tu rutina...";

            try
            {
                long userId = Preferences.Get("UserId", 0L);
                long responseId = Preferences.Get("responseId", 0L);
                string coachType = Preferences.Get("CoachModelTypeName", string.Empty);
                string? note = string.IsNullOrWhiteSpace(UserNote) ? null : UserNote.Trim();

                var newRoutine = await _aiRoutineService.GenerateRoutineAsync(
                    userId.ToString(), responseId, coachType, note);

                if (newRoutine == null)
                {
                    await NotificationService.ShowErrorAsync("No se pudo regenerar la rutina. Intenta de nuevo.");
                    return;
                }

                Routine = newRoutine;
                UserNote = string.Empty;
                await NotificationService.ShowSuccessAsync("¡Rutina regenerada!");
            }
            catch (Exception ex)
            {
                await NotificationService.ShowErrorAsync($"Error al regenerar: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>Guarda la rutina generada en el perfil del usuario.</summary>
        [RelayCommand]
        private async Task SaveRoutineAsync()
        {
            if (Routine == null)
            {
                await NotificationService.ShowErrorAsync("No hay ninguna rutina para guardar.");
                return;
            }

            IsLoading = true;
            IsSaving = true;
            StatusMessage = "Guardando tu rutina...";

            try
            {
                long userId = Preferences.Get("UserId", 0L);

                var response = await _trainingService.createRoutineAsync(userId, Routine);

                if (response == null)
                {
                    await NotificationService.ShowErrorAsync(
                        "No se pudo guardar tu rutina. Por favor, intenta nuevamente.");
                    return;
                }

                var registrationResult = await _appUserService.MarkRegistrationComplete(userId);
                if (registrationResult == null)
                    Debug.WriteLine("MarkRegistrationComplete falló, el usuario será redirigido al cuestionario en el próximo login.");

                await NotificationService.ShowSuccessAsync("¡Tu rutina ha sido guardada correctamente!");
                StatusMessage = "Cargando tu rutina guardada...";
                await Shell.Current.GoToAsync("///HomeView", false);
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync($"Error al guardar la rutina: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        #endregion
    }
}
