using CommunityToolkit.Mvvm.ComponentModel;
using IFit.Helper;
using IFit.Models;
using IFit.Models.Dtos.ExperienceLevel;
using IFit.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IFit.ViewModels
{
    public partial class ExperienceLevelViewModel : ObservableObject
    {
        #region Services

        private DatabaseService? databaseService = App.GetService<DatabaseService>();
        private AppUserService? appUserService = App.GetService<AppUserService>();
        private ExperienceLevelService? experienceLevelService = App.GetService<ExperienceLevelService>();

        #endregion

        #region Properties

        [ObservableProperty]
        public partial List<ExperienceLevelDto>? ExperienceLevels { get; set; }
        partial void OnSelectedExperienceLevelChanged(ExperienceLevelDto? value)
        {
            _ = HandleExperienceLevelChangedAsync(value);
        }

        [ObservableProperty]
        public partial ExperienceLevelDto? SelectedExperienceLevel { get; set; }

        #endregion

        #region States

        [ObservableProperty]
        public partial bool isLoading { get; set; } = false;

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor con inyección de dependencias para servicios
        /// </summary>    
        public ExperienceLevelViewModel(DatabaseService databaseService,
                                        AppUserService appUserService,
                                        ExperienceLevelService experienceLevelService)
        {
            this.databaseService = databaseService;
            this.appUserService = appUserService;
            this.experienceLevelService = experienceLevelService;
        }

        public ExperienceLevelViewModel() : this(
                 App.GetService<DatabaseService>() ?? throw new InvalidOperationException("DatabaseService no registrado"),
                 App.GetService<AppUserService>() ?? throw new InvalidOperationException("AppUserService no registrado"), 
                 App.GetService<ExperienceLevelService>() ?? throw new InvalidOperationException("ExperienceLevelService no registrado"))
        {
            _ = InitializeAsync();
        }

        #endregion

        #region InitializeAsync

        public async Task InitializeAsync()
        {
            isLoading = true;
            StatusMessage = "Cargando niveles de experiencia...";

            await LoadExperienceLevels();

            isLoading = false;
            StatusMessage = string.Empty;
        }

        #endregion

        #region Methods

        private async Task LoadExperienceLevels()
        {

            //Check if services are initialized.
            if (experienceLevelService == null)
            {
                await ErrorHandler.HandleErrorAsync("Los servicios no se han inicializado. Por favor, inténtelo más tarde.", "///ErrorView");
                return;
            }

           ExperienceLevels = await experienceLevelService.GetExperienceLevels();

            if(ExperienceLevels == null || !ExperienceLevels.Any())
            {
                await ErrorHandler.HandleErrorAsync("Ha ocurrido un error durante el proceso.  Por favor, inténtelo más tarde.", "///ErrorView");
                return;
            }
        }

        private async Task HandleExperienceLevelChangedAsync(ExperienceLevelDto? value)
        {
            if (value == null || databaseService == null || appUserService == null)
            {
                await ErrorHandler.HandleErrorAsync("Selected experience level type is null or services are not initialized.", "//ErrorView");
                return;
            }

            var selectedLevel = ExperienceLevels?.FirstOrDefault(c => c.Name == value.Name);
            if (selectedLevel == null)
            {
                await ErrorHandler.HandleErrorAsync("Selected experience level type not found in the list.", "//ErrorView");
                return;
            }

            AppUser? user = await databaseService.GetCurrentUserAsync();
            if (user == null)
            {
                await ErrorHandler.HandleErrorAsync("No user found in the database.", "//ErrorView");
                return;
            }

            AppUserResponseDto? response = await appUserService.SetExperienceLevel(user.Id, selectedLevel.Id);
            if (response == null
                && string.IsNullOrEmpty(response?.ExperienceLevelName))
            {
                await ErrorHandler.HandleErrorAsync("Failed to set experience level type.", "//ErrorView",
                    "Error",
                    "No se pudo establecer el tipo de modelo de entrenador. Por favor, inténtelo más tarde.");
                return;
            }
            Preferences.Set("ExperienceLevelId", selectedLevel.Id);
            Preferences.Set("ExperienceName", selectedLevel.Name);

            await databaseService.SaveAppUserAsync(response.toEntity());
            await Shell.Current.GoToAsync("CoachModelTypeSelectionView");
        }

        #endregion
    }
}
