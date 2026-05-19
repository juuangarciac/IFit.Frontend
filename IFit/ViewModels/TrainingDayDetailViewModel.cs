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
        private TrainingService _trainingService;

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

        partial void OnTrainingDayChanged(TrainingDayDto value)
        {
            ExerciseCount = value?.Exercises?.Count ?? 0;
            EstimatedDuration = CalculateEstimatedDuration(value);
            CanEndSession = Routine.CurrentDay == value.DayNumber;
            Background = GetBrush(Routine.CurrentDay >= value.DayNumber
                ? "CardPremiumGradientColor"
                : "CardPremiumIndigoGradientColor");

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

        public TrainingDayDetailViewModel(TrainingService trainingService) 
        { 
            _trainingService = trainingService;

            // Set the current date formatted as "d 'de' MMMM 'de' yyyy" in Spanish 
            DateNowFormatted = DateTime.Now.ToString("d 'de' MMMM 'de' yyyy", new CultureInfo("es-ES"));
        }

        public TrainingDayDetailViewModel() : this(
            App.GetService<TrainingService>() ?? throw new InvalidOperationException("TrainingService no registrado")) 
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
