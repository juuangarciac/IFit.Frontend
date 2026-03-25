using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos.AI;
using IFit.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.ViewModels
{
    public partial class WeeklySummaryViewModel : ObservableObject
    {
        #region Properties
        [ObservableProperty]
        public partial RoutineResponseDto? Routine { get; set; }

        [ObservableProperty]
        public partial TrainingDayDto? TrainingDayDto { get; set; }
        partial void OnTrainingDayDtoChanged(TrainingDayDto? value)
        {
            if (value is null) return;
            _ = NavigateToDetailAsync(value);
        }

        [ObservableProperty]
        public partial Boolean IsLoading { get; set; } = true;

        [ObservableProperty]
        public partial String StatusMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial ObservableCollection<TrainingDayDto> CompletedSessions { get; set; } = new();

        [ObservableProperty]
        public partial ObservableCollection<TrainingDayDto> UncompletedSessions { get; set; } = new();
        #endregion

        #region Services
        private AppUserService _appUserService;
        private TrainingService _trainingService;
        #endregion

        #region Constructor
        public WeeklySummaryViewModel(TrainingService trainingService,
            AppUserService appUserService)
        {
            _trainingService = trainingService;
            _appUserService = appUserService;
        }

        public WeeklySummaryViewModel() : this(
            App.GetService<TrainingService>() ?? throw new InvalidOperationException("TrainingService no registrado"),
            App.GetService<AppUserService>() ?? throw new InvalidOperationException("AppUserService no esta registrado"))
        {
        }

        private async Task InitializeAsync()
        {
            IsLoading = true;
            StatusMessage = "Cargando tu rutina actual...";

            var userId = Preferences.Get("UserId", 0L);

            AppUserResponseDto? currentUser = await _appUserService.findUserById(userId);
            if (currentUser == null)
            {
                StatusMessage = "No se ha encontrado el usuario actual.";
                return;
            }

            Routine = await _trainingService.getLatestActiveRoutineByUserIdAsync(userId);

            if (Routine == null)
            {
                StatusMessage = "No se ha encontrado la rutina actual.";
                return;
            }

            BuildSessionLists();

            IsLoading = false;
        }
        #endregion

        #region Commands

        #endregion

        #region Methods

        private void BuildSessionLists()
        {
            CompletedSessions.Clear();
            UncompletedSessions.Clear();

            if (Routine?.Days == null) return;

            int currentDay = Routine.CurrentDay ?? 0;

            foreach (var day in Routine.Days.OrderBy(d => d.DayNumber))
            {
                if (day.DayNumber <= currentDay)
                    CompletedSessions.Add(day);
                else
                    UncompletedSessions.Add(day);
            }
        }

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

        private async Task NavigateToDetailAsync(TrainingDayDto value)
        {
            var navigationParameter = new Dictionary<string, object>()
            {
                { "Routine", Routine },
                { "TrainingDay", value },
                { "PreviousPage", "WeeklySummaryView" }
            };
            await Shell.Current.GoToAsync($"//TrainingDayDetailView", navigationParameter);
        }

        #endregion
    }
}