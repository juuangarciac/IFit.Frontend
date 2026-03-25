using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models;
using IFit.Models.Dtos.AI;
using IFit.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.ViewModels
{
    [QueryProperty(nameof(User), "User")]
    [QueryProperty(nameof(Routine), "Routine")]
    public partial class PlanViewModel : ObservableObject
    {

        #region Services

        private AppUserService appUserService;

        private QuestionnaireService questionnaireService;

        private TrainingService trainingService;

        #endregion

        #region Properties

        [ObservableProperty]
        public partial AppUserResponseDto? User { get; set; }

        [ObservableProperty]
        public partial RoutineResponseDto? Routine { get; set; }

        [ObservableProperty]
        public partial List<TrainingDayDto?>? Days { get; set; }

        [ObservableProperty]
        public partial String StatusMessage { get; set; } = string.Empty;

        #endregion

        #region Constructor

        public PlanViewModel(AppUserService appUserService, QuestionnaireService questionnaireService, TrainingService trainingService)
        {
            this.appUserService = appUserService;
            this.questionnaireService = questionnaireService;
            this.trainingService = trainingService;
        }

        public PlanViewModel() : this(
            App.GetService<AppUserService>() ?? throw new InvalidOperationException("AppUserService no registrado"),
            App.GetService<QuestionnaireService>() ?? throw new InvalidOperationException("QuestionnaireService no registrado"),
            App.GetService<TrainingService>() ?? throw new InvalidOperationException("TrainingService no registrado")
        )
        {

        }

        private async Task InitializeAsync()
        {
            if(User == null)
            {
                var userId = Preferences.Get("UserId", 0L);
                User = await appUserService.findUserById(userId);

                if (User == null)
                {
                    StatusMessage = "No se ha encontrado el usuario.";
                    return;
                }
            }

            if(Routine == null)
            {
                Routine = await trainingService.getLatestActiveRoutineByUserIdAsync(User.Id);

                if (Routine == null)
                {
                    StatusMessage = "No se ha encontrado la rutina actual.";
                    return;
                }

                Days = Routine.Days.OrderBy(d => d.DayNumber).ToList();
            }
        }

        #endregion

        #region Commands

        [RelayCommand]
        public async Task AppearingAsync()
        {
            _ = InitializeAsync();
        }

        [RelayCommand]
        public async Task GenerateNewRoutineAsync()
        {
            await Shell.Current.GoToAsync("ExperienceLevelSelectionView");
        }

        [RelayCommand]
        public async Task SetRoutineAsCompleted()
        {
            if (Routine == null || User == null) return;
            var result = await trainingService.completeRoutineAsync((long)Routine.Id);
            if (result != null)
            {
                StatusMessage = "¡Rutina marcada como completada!";
                await InitializeAsync();
            }
            else
            {
                StatusMessage = "Error al marcar la rutina como completada.";
            }
        }

        #endregion
    }
}
