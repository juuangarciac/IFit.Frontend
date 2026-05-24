using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos.AI;
using IFit.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IFit.ViewModels
{
    [QueryProperty(nameof(Routine), "Routine")]
    [QueryProperty(nameof(TrainingDay), "TrainingDay")]
    public partial class TrainingDayDetailViewModel : ObservableObject
    {
        #region Services
        private readonly TrainingService _trainingService;
        private readonly AIRoutineService _aiRoutineService;
        private readonly ExerciseCatalogService _exerciseCatalogService;
        #endregion

        #region Properties
        [ObservableProperty]
        public partial RoutineResponseDto Routine { get; set; }

        [ObservableProperty]
        public partial TrainingDayDto TrainingDay { get; set; }

        [ObservableProperty]
        public partial string DateNowFormatted { get; set; }

        [ObservableProperty]
        public partial string EstimatedDuration { get; set; }

        [ObservableProperty]
        public partial Boolean CanEndSession { get; set; } = false;

        [ObservableProperty]
        public partial Boolean IsLoading { get; set; } = false;

        [ObservableProperty]
        public partial String StatusMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial int ExerciseCount { get; set; }

        [ObservableProperty]
        public partial string SessionStatusMessage { get; set; } = string.Empty;

        private bool _isExplaining = false;

        [ObservableProperty]
        public partial bool IsExplanationModalVisible { get; set; } = false;

        [ObservableProperty]
        public partial bool IsExplanationLoading { get; set; } = false;

        [ObservableProperty]
        public partial string ExplanationExerciseName { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string ExplanationText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsCatalogNotFoundModalVisible { get; set; } = false;

        [ObservableProperty]
        public partial string CatalogNotFoundExerciseName { get; set; } = string.Empty;

        [ObservableProperty]
        public partial ExerciseDto? SelectedExercise { get; set; }

        partial void OnTrainingDayChanged(TrainingDayDto value)
        {
            ExerciseCount = value?.Exercises?.Count ?? 0;
            EstimatedDuration = CalculateEstimatedDuration(value);
            CanEndSession = Routine.CurrentDay == value.DayNumber;
            Background = GetBrush(Routine.CurrentDay >= value.DayNumber
                ? "CardPremiumGradientColor"
                : "CardPremiumOrangeGradientColor");

            if (Routine.CurrentDay > value.DayNumber)
                SessionStatusMessage = "Sesión ya completada";
            else if (Routine.CurrentDay < value.DayNumber)
                SessionStatusMessage = "Aún no has llegado a este entrenamiento";
            else
                SessionStatusMessage = string.Empty;
        }

        private static Brush GetBrush(string resourceKey)
        {
            if (Application.Current!.Resources.TryGetValue(resourceKey, out var resource) && resource is Brush brush)
                return brush;
            return new SolidColorBrush(Colors.Transparent);
        }

        [ObservableProperty]
        public partial Brush? Background { get; set; }

        #endregion

        #region Constructor

        public TrainingDayDetailViewModel(
            TrainingService trainingService,
            AIRoutineService aiRoutineService,
            ExerciseCatalogService exerciseCatalogService)
        {
            _trainingService        = trainingService;
            _aiRoutineService       = aiRoutineService;
            _exerciseCatalogService = exerciseCatalogService;
            DateNowFormatted        = DateTime.Now.ToString("d 'de' MMMM 'de' yyyy", new CultureInfo("es-ES"));
        }

        public TrainingDayDetailViewModel() : this(
            App.GetService<TrainingService>()         ?? throw new InvalidOperationException("TrainingService no registrado"),
            App.GetService<AIRoutineService>()        ?? throw new InvalidOperationException("AIRoutineService no registrado"),
            App.GetService<ExerciseCatalogService>()  ?? throw new InvalidOperationException("ExerciseCatalogService no registrado"))
        {
        }

        #endregion

        #region Commands

        [RelayCommand]
        public async Task EndSessionAsync()
        {
            IsLoading = true;
            StatusMessage = "Guardando sesión...";
            try
            {
                var response = await _trainingService.setRoutineDayAsCompletedAsync(
                    (long)Routine.Id, (int)Routine.CurrentDay);

                if (response == null)
                {
                    await NotificationService.ShowErrorAsync("No se pudo finalizar la sesión.");
                    return;
                }

                Preferences.Remove("CurrentRoutineId");
                await NotificationService.ShowSuccessAsync("¡Sesión guardada correctamente!");
                await Shell.Current.GoToAsync("//HomeView", false);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        [RelayCommand]
        public async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task ExplainExerciseAsync(ExerciseDto exercise)
        {
            if (exercise == null || _isExplaining) return;

            _isExplaining             = true;
            ExplanationExerciseName   = exercise.ExerciseName;
            ExplanationText           = string.Empty;
            IsExplanationLoading      = true;
            IsExplanationModalVisible = true;

            try
            {
                var userId         = Preferences.Get("UserId", 0L);
                var coachName      = Preferences.Get("CoachName", "ronnie");
                var experienceName = Preferences.Get("ExperienceName", string.Empty);
                if (!AIRoutineService.IsValidCoach(coachName)) coachName = "ronnie";

                var explanation = await _aiRoutineService.GetExerciseExplanationAsync(
                    exercise.ExerciseName, exercise.Sets, exercise.Reps,
                    exercise.RestSeconds, userId, coachName, experienceName);

                ExplanationText = explanation ?? "No se pudo obtener la explicación del ejercicio.";
            }
            catch (Exception ex)
            {
                ExplanationText = "Error al obtener la explicación.";
                Debug.WriteLine($"✗ ExplainExercise: {ex.Message}");
            }
            finally
            {
                IsExplanationLoading = false;
                _isExplaining        = false;
            }
        }

        [RelayCommand]
        public async Task OpenInCatalogAsync(ExerciseDto exercise)
        {
            if (exercise == null || IsLoading) return;

            IsLoading     = true;
            StatusMessage = "Buscando en catálogo...";
            var showNotFound = false;
            try
            {
                var result = await _exerciseCatalogService.GetExercisesAsync(
                    page: 0, size: 1, muscle: exercise.ExerciseName);

                if (result?.Content?.Count > 0)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "InitialSearch", exercise.ExerciseName }
                    };
                    await Shell.Current.GoToAsync("ExerciseCatalogView", parameters);
                }
                else
                {
                    showNotFound = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ OpenInCatalog: {ex.Message}");
                showNotFound = true;
            }
            finally
            {
                IsLoading     = false;
                StatusMessage = string.Empty;
            }

            if (showNotFound)
            {
                CatalogNotFoundExerciseName   = exercise.ExerciseName;
                IsCatalogNotFoundModalVisible = true;
            }
        }

        [RelayCommand]
        public void CloseCatalogNotFound()
        {
            IsCatalogNotFoundModalVisible = false;
            CatalogNotFoundExerciseName   = string.Empty;
        }

        [RelayCommand]
        public async Task OpenCatalogAnywayAsync()
        {
            IsCatalogNotFoundModalVisible = false;
            await Shell.Current.GoToAsync("ExerciseCatalogView");
        }

        [RelayCommand]
        public void CloseExplanation()
        {
            IsExplanationModalVisible  = false;
            ExplanationText            = string.Empty;
            ExplanationExerciseName    = string.Empty;
        }

        #endregion

        #region Methods

        private string CalculateEstimatedDuration(TrainingDayDto trainingDay)
        {
            if (trainingDay?.Exercises == null || trainingDay.Exercises.Count == 0)
                return "~30 min";

            const int secondsPerSet = 50;     // tiempo de ejecución por serie
            const int transitionSeconds = 90; // transición/calentamiento entre ejercicios

            int totalSeconds = 0;

            foreach (var exercise in trainingDay.Exercises)
            {
                int sets = exercise.Sets ?? 0;
                int rest = exercise.RestSeconds ?? 60;

                if (sets <= 0) continue;

                int executionTime = sets * secondsPerSet;
                int restBetweenSets = (sets - 1) * rest;

                totalSeconds += executionTime + restBetweenSets + transitionSeconds;
            }

            int minutes = Math.Max(totalSeconds / 60, 30);

            if (minutes < 60)
                return $"~{minutes} min";

            int hours = minutes / 60;
            int remainingMinutes = minutes % 60;
            return remainingMinutes > 0 ? $"~{hours}h {remainingMinutes}min" : $"~{hours}h";
        }

        #endregion
    }
}
