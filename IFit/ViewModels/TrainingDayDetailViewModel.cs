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

        partial void OnTrainingDayChanged(TrainingDayDto value)
        {
            EstimatedDuration = CalculateEstimatedDuration(value);
        }

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
            Debug.Write($"End Session");
            var response = await _trainingService.setRoutineDayAsCompletedAsync((long)Routine.Id, (int)Routine.CurrentDay);
            if(response == null)
            {
                await Shell.Current.DisplayAlert(
                        "Error",
                        "No se pudo finalizar la sesión.",
                        "OK"
                    );
                return;
            }

            await Shell.Current.DisplayAlert(
                        "Exito",
                        "Se ha guardado correctamente la sesión.",
                        "OK"
                    );

            await Shell.Current.GoToAsync("///HomeView");
        }

        #endregion

        #region Methods

        private string CalculateEstimatedDuration(TrainingDayDto trainingDay)
        {
            if (trainingDay?.Exercises == null || trainingDay.Exercises.Count == 0)
                return "N/A";

            const int secondsPerSet = 40; // tiempo estimado por serie (ejecución)

            int totalSeconds = 0;

            foreach (var exercise in trainingDay.Exercises)
            {
                int sets = exercise.Sets ?? 0;
                int rest = exercise.RestSeconds ?? 60;

                if (sets <= 0) continue;

                int executionTime = sets * secondsPerSet;
                int restTime = (sets - 1) * rest;

                totalSeconds += executionTime + restTime;
            }

            // Formato legible
            int minutes = totalSeconds / 60;
            if (minutes < 60)
                return $"~{minutes} min";

            int hours = minutes / 60;
            int remainingMinutes = minutes % 60;
            return remainingMinutes > 0 ? $"~{hours}h {remainingMinutes}min" : $"~{hours}h";
        }

        #endregion
    }
}
