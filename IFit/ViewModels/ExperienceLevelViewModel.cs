using IFit.Helper;
using IFit.Models;
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
    public class ExperienceLevelViewModel : INotifyPropertyChanged
    {
        private DatabaseService? databaseService = App.GetService<DatabaseService>();
        private AppUserService? appUserService = App.GetService<AppUserService>();
        private ExperienceLevelService? experienceLevelService = App.GetService<ExperienceLevelService>();

        public ExperienceLevelViewModel()
        {
            LoadExperienceLevels();
        }

        private List<ExperienceLevel>? experienceLevels = new List<ExperienceLevel>();
        public List<ExperienceLevel>? ExperienceLevels
        {
            get { return experienceLevels; }
            set
            {
                if (experienceLevels != value)
                {
                    experienceLevels = value;
                    OnPropertyChanged(nameof(ExperienceLevels));
                }
            }
        }

        private async void LoadExperienceLevels()
        {
            //Check if services are initialized.
            if(experienceLevelService == null)
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

        private ExperienceLevel? selectedExperienceLevel;
        public ExperienceLevel? SelectedExperienceLevel
        {
            get { return selectedExperienceLevel; }
            set
            {
                if (selectedExperienceLevel != value)
                {
                    selectedExperienceLevel = value;
                    OnPropertyChanged(nameof(SelectedExperienceLevel));
                    OnSelectedExperienceLevelChanged(selectedExperienceLevel);
                }
            }
        }

        private async void OnSelectedExperienceLevelChanged(ExperienceLevel? selectedExperienceLevel)
        {
            if (selectedExperienceLevel == null || databaseService == null || appUserService == null)
            {
                await ErrorHandler.HandleErrorAsync("Selected experience level type is null or services are not initialized.", "//ErrorView");
                return;
            }

            selectedExperienceLevel = ExperienceLevels?.FirstOrDefault(c => c.Name == selectedExperienceLevel.Name);
            if (selectedExperienceLevel == null)
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

            AppUser? result = await appUserService.SetExperienceLevel(user.Id, selectedExperienceLevel.Id);
            if (result == null)
            {
                await ErrorHandler.HandleErrorAsync("Failed to set experience level type.", "//ErrorView",
                    "Error",
                    "No se pudo establecer el tipo de modelo de entrenador. Por favor, inténtelo más tarde.");
                return;
            }

            await databaseService.SaveAppUserAsync(result);
            await Shell.Current.GoToAsync("//AppUserQuestionnaireView");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
