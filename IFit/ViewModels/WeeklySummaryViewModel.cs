using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos.AI;
using IFit.Services;
using IFit.Views.Components;
using System.Collections.ObjectModel;

namespace IFit.ViewModels
{
    [QueryProperty(nameof(Routine), "Routine")]
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

        [ObservableProperty]
        public partial string SessionProgress { get; set; } = "0/0";

        [ObservableProperty]
        public partial int TotalExercises { get; set; } = 0;

        [ObservableProperty]
        public partial int TotalSets { get; set; } = 0;

        [ObservableProperty]
        public partial string EstimatedTime { get; set; } = "0 min";

        [ObservableProperty]
        public partial bool HasCompletedSessions { get; set; } = false;

        [ObservableProperty]
        public partial BarChartDrawable? BarChart { get; set; }

        #endregion

        #region Services
        private TrainingService _trainingService;
        #endregion

        private bool _isInitialized = false;

        #region Constructor

        public WeeklySummaryViewModel(TrainingService trainingService)
        {
            _trainingService = trainingService;
        }

        public WeeklySummaryViewModel() : this(
            App.GetService<TrainingService>() ?? throw new InvalidOperationException("TrainingService no registrado"))
        {
        }

        private async Task InitializeAsync()
        {
            IsLoading = true;
            StatusMessage = "Cargando tu rutina actual...";

            var userId = Preferences.Get("UserId", 0L);
            if (userId == 0)
            {
                StatusMessage = "No se ha encontrado el usuario actual.";
                IsLoading = false;
                return;
            }

            var cachedRoutineId = Preferences.Get("CurrentRoutineId", 0L);
            Routine = cachedRoutineId > 0
                ? await _trainingService.getRoutineByIdAsync(cachedRoutineId)
                : await _trainingService.getLatestActiveRoutineByUserIdAsync(userId);

            if (Routine == null)
            {
                StatusMessage = "No se ha encontrado la rutina actual.";
                IsLoading = false;
                return;
            }

            BuildSessionLists();
            IsLoading = false;
        }

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

            ComputeStats();
        }

        private void ComputeStats()
        {
            SessionProgress = $"{CompletedSessions.Count}/{Routine?.TrainingDays ?? 0}";

            int exercises = 0, sets = 0, seconds = 0;
            foreach (var day in CompletedSessions)
            {
                exercises += day.Exercises.Count;
                foreach (var ex in day.Exercises)
                {
                    int s = ex.Sets ?? 0;
                    sets += s;
                    seconds += s * (30 + (ex.RestSeconds ?? 60));
                }
            }

            TotalExercises = exercises;
            TotalSets = sets;
            EstimatedTime = $"{seconds / 60} min";

            BuildChart();
        }

        private void BuildChart()
        {
            HasCompletedSessions = CompletedSessions.Count > 0;
            if (!HasCompletedSessions) return;

            var values = CompletedSessions
                .Select(d => (double)d.Exercises.Count)
                .ToArray();

            var labels = CompletedSessions
                .Select(d => d.DayName.Length > 9 ? d.DayName[..9] + "." : d.DayName)
                .ToArray();

            BarChart = new BarChartDrawable(values, labels);
        }

        [RelayCommand]
        public async Task AppearingAsync()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            if (Routine != null)
            {
                // Routine llegó vía QueryProperty desde HomeViewModel: sin llamada HTTP
                BuildSessionLists();
                IsLoading = false;
                return;
            }

            await InitializeAsync();
        }

        private async Task NavigateToDetailAsync(TrainingDayDto value)
        {
            var navigationParameter = new Dictionary<string, object>()
            {
                { "Routine", Routine! },
                { "TrainingDay", value }
            };
            await Shell.Current.GoToAsync("TrainingDayDetailView", navigationParameter);
            TrainingDayDto = null; // permite re-seleccionar el mismo día al volver
        }

        #endregion
    }
}
