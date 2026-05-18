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

        #endregion

        #region Properties

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

        public PlanSummaryViewModel(TrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
        }

        public PlanSummaryViewModel() : this(
            App.GetService<TrainingService>() ?? throw new InvalidOperationException("TrainingService no registrado"))
        {
        }

        #endregion

        #region Commands

        [RelayCommand]
        public Task AppearingAsync()
        {
            if (_isInitialized) return Task.CompletedTask;
            // Fire-and-forget: la vista aparece inmediatamente, los datos cargan en segundo plano
            _ = LoadDataAsync();
            return Task.CompletedTask;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Cargando rutinas...";

                var userId = Preferences.Get("UserId", 0L);

                // Ambas llamadas en paralelo
                var allRoutinesTask   = _trainingService.getRoutinesByUserIdAsync(userId);
                var activeRoutineTask = _trainingService.getLatestActiveRoutineByUserIdAsync(userId);
                await Task.WhenAll(allRoutinesTask, activeRoutineTask);

                var allRoutines   = allRoutinesTask.Result;
                var activeRoutine = activeRoutineTask.Result;

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
                Debug.WriteLine($"[ERROR] PlanSummary LoadDataAsync => {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        [RelayCommand]
        private async Task ActivateRoutineAsync(RoutineResponseDto routine)
        {
            if (routine is null) return;

            IsLoading = true;
            StatusMessage = "Actualizando rutina activa...";

            try
            {
                var result = await _trainingService.toggleRoutineActiveAsync((long)routine.Id, true);
                if (result == null)
                {
                    await NotificationService.ShowErrorAsync("No se pudo activar la rutina. Inténtalo de nuevo.");
                    return;
                }

                await NotificationService.ShowSuccessAsync("¡Rutina activada correctamente!");
                _isInitialized = false;
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] ActivateRoutineAsync => {ex.Message}");
                await NotificationService.ShowErrorAsync("Error inesperado al activar la rutina.");
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
            if (ActiveRoutines?.Count > 0)
                await NotificationService.ShowInfoAsync("Al generar una nueva rutina, tu rutina actual quedará desactivada automáticamente.");

            await Shell.Current.GoToAsync("CoachModelTypeSelectionView");
        }

        #endregion

        #region Methods

        private async Task OnSelectedRoutineAsync()
        {
            if (SelectedRoutine is null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Routine", SelectedRoutine }
                // User no se pasa: PlanViewModel lo obtiene de Preferences si es null
            };

            await Shell.Current.GoToAsync("//PlanView", parameters);
        }

        #endregion
    }
}
