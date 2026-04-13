using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos.AI;
using IFit.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.ViewModels
{
    public partial class PlanSummaryViewModel : ObservableObject
    {
        #region Services

        private TrainingService _trainingService;

        private AppUserService _appUserService;

        #endregion

        #region Properties

        [ObservableProperty]
        public partial String User { get; set; }

        [ObservableProperty]
        public partial List<RoutineResponseDto> AllRoutines { get; set; }

        [ObservableProperty]
        public partial List<RoutineResponseDto> ActiveRoutines { get; set; }

        [ObservableProperty]
        public partial Boolean IsLoading { get; set; } = false;

        [ObservableProperty]
        public partial String StatusMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial String AllRoutinesMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial String ActiveRoutinesMessage { get; set; }

        private RoutineResponseDto _selectedRoutine;
        public RoutineResponseDto SelectedRoutine
        {
            get => _selectedRoutine;
            set
            {
                if (SetProperty(ref _selectedRoutine, value) && value != null)
                {
                    _ = OnSelectedRoutineAsync();
                }
            }
        }

        #endregion

        private bool _isInitialized = false;

        #region Constructor

        public PlanSummaryViewModel(TrainingService trainingService, AppUserService appUserService)
        {
            this._trainingService = trainingService;
            this._appUserService = appUserService;;
        }

        public PlanSummaryViewModel() : this(
         App.GetService<TrainingService>() ?? throw new InvalidOperationException("TrainingService no registrado"),
         App.GetService<AppUserService>() ?? throw new InvalidOperationException("AppUserService no esta registrado"))
        {
        }

        #endregion

        #region Commands

        [RelayCommand]
        public async Task AppearingAsync()
        {
            if (_isInitialized) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Cargando rutina...";

                var userId = Preferences.Get("UserId", 0L);

                // Fetch all routines and active routine in parallel
                var allRoutinesTask   = _trainingService.getRoutinesByUserIdAsync(userId);
                var activeRoutineTask = _trainingService.getLatestActiveRoutineByUserIdAsync(userId);
                await Task.WhenAll(allRoutinesTask, activeRoutineTask);

                var allRoutines   = await allRoutinesTask;
                var activeRoutine = await activeRoutineTask;

                AllRoutines = allRoutines is { Count: > 0 }
                    ? allRoutines
                    : new List<RoutineResponseDto>();

                if (AllRoutines.Count == 0)
                    AllRoutinesMessage = "No se han encontrado rutinas para este usuario.";

                ActiveRoutines = activeRoutine is not null
                    ? new List<RoutineResponseDto> { activeRoutine }
                    : new List<RoutineResponseDto>();

                if (ActiveRoutines.Count == 0)
                    ActiveRoutinesMessage = "No se han encontrado rutinas activas para este usuario.";

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] AppearingAsync => {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        [RelayCommand]
        public async Task GenerateNewRoutineAsync()
        {
            await Shell.Current.GoToAsync("//CoachModelTypeSelectionView");
        }

        #endregion

        #region Methods

        private async Task OnSelectedRoutineAsync()
        {

            var parameters = new Dictionary<string, object>
                {
                    { "Routine", SelectedRoutine },
                    { "User", User }
                };

            await Shell.Current.GoToAsync("//PlanView", parameters);

        }

        #endregion
    }
}
