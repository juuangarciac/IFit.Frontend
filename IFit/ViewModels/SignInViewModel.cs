using IFit;
using IFit.Helper;
using IFit.Models;
using IFit.Models.Dtos;
using IFit.Services;
using System.Diagnostics;
using System.Windows.Input;

namespace IFit.ViewModels
{
    public class SignInViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        private readonly AuthenticationService? authenticationService;
        private readonly AppUserService? appUserService;
        private DatabaseService? databaseService;
        private AppUserAnswerService? appUserAnswerService;
        private AppUserQuestionnaireService? appUserQuestionnaireService;

        public ICommand LoginCommand { get; }

        public SignInViewModel()
        {
            authenticationService = App.GetService<AuthenticationService>();
            appUserService = App.GetService<AppUserService>();
            databaseService = App.GetService<DatabaseService>();
            appUserAnswerService = App.GetService<AppUserAnswerService>();
            appUserQuestionnaireService = App.GetService<AppUserQuestionnaireService>();

            LoginCommand = new Command(async () => await SignInAsync());
        }

        public async Task SignInAsync()
        {
            if (!ValidateInputs()) return;

            var signInResponseDto = await TryLoginAsync();
            if (signInResponseDto == null) return;

            SaveLoginData(signInResponseDto);

            var appUser = await LoadUserDataAsync();
            if (appUser == null) return;

            await InsertUserToDatabase(appUser);

            // If the username is null or empty, it indicates that the user has not completed the initial setup
            if (!appUser.RegistrationComplete)
            {
                if (!await HandleVerificationAsync(appUser)) return; // Verify email first
                if (!await HandleCoachSelectionAsync(appUser)) return; // Then select coach model type
                if (!await HandleExperienceLevelSelectionAsync(appUser)) return; // Then select experience level
                if (!await HandleAppUserQuestionnaireAndQuestionsAnswered(appUser)) return; // Then complete questionnaire
            }

            await Shell.Current.GoToAsync("///HomeView");
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                _ = ErrorHandler.HandleErrorAsync("Ups!", "Por favor, ingrese su correo electrónico y contraseña.");
                return false;
            }
            return true;
        }

        private async Task<SignInResponseDto?> TryLoginAsync()
        {
            if(authenticationService == null)
            {
                await ErrorHandler.HandleErrorAsync("Ups!", "No se pudo iniciar sesión. Por favor, intente nuevamente más tarde.");
                return null;
            }

            var response = await authenticationService.LoginAsync(Email, Password);
            if (response == null)
            {
                await ErrorHandler.HandleErrorAsync("Ups!", "No se pudo iniciar sesión. Por favor, verifique sus credenciales.");
            }
            // return response;
            return null;
        }

        private void SaveLoginData(SignInResponseDto dto)
        {
            Preferences.Set("UserEmail", Email);
            Preferences.Set("UserToken", dto.token);
            Preferences.Set("UserAuthorities", dto.authorities);
        }

        private async Task<AppUser?> LoadUserDataAsync()
        {
            if(appUserService == null)
            {
                await ErrorHandler.HandleErrorAsync("Ups!", "No se pudo recuperar la información del usuario. Por favor, intente nuevamente.");
                await Shell.Current.GoToAsync("///ErrorView");
                return null;
            }

            var user = await appUserService.findUserByEmail(Email);
            if (user == null)
            {
                await ErrorHandler.HandleErrorAsync("Ups!", "No se pudo recuperar la información del usuario. Por favor, intente nuevamente.");
                await Shell.Current.GoToAsync("///ErrorView");
            }
            Preferences.Set("UserId", user?.Id ?? 0);

            // return user;
            return null;
        }

        private async Task<bool> HandleVerificationAsync(AppUser appUser)
        {

            if(authenticationService == null) {
                await ErrorHandler.HandleErrorAsync("Ups!", "No se pudo verificar el estado del usuario. Por favor, intente nuevamente más tarde.");
                await Shell.Current.GoToAsync("///ErrorView");
                return false;
            }

            if (!appUser.Verified)
            {
                // ErrorHandler.HandleErrorAsync("Parece que se le olvido algo la última vez!", "Por favor, verifique su correo electrónico antes de continuar.");
               //  await authenticationService.SendVerificationEmail(Email);
                await Shell.Current.GoToAsync("///VerificationView");
                return false;
            }

            Preferences.Set("IsVerified", appUser.Verified);
            return true;
        }

        private async Task<bool> HandleCoachSelectionAsync(AppUser appUser)
        {
            if (!string.IsNullOrEmpty(appUser.CoachModelTypeName))
            {
                // ErrorHandler.HandleErrorAsync("Parece que se le olvido algo la última vez!", "Por favor, seleccione un tipo de modelo de entrenador antes de continuar.");
                await Shell.Current.GoToAsync("///GetStartedView");
                return false;
            }

            Preferences.Set("CoachModelTypeName", appUser.CoachModelTypeName);
            return true;
        }

        private async Task<bool> HandleExperienceLevelSelectionAsync(AppUser appUser)
        {
            if (!string.IsNullOrEmpty(appUser.ExperienceLevelName))
            {
                // ErrorHandler.HandleErrorAsync("Parece que se le olvido algo la última vez!", "Por favor, seleccione un tipo de modelo de entrenador antes de continuar.");
                await Shell.Current.GoToAsync("///GetStartedView");
                return false;
            }

            Preferences.Set("ExperienceLevelName", appUser.ExperienceLevelName);
            return true;
        }

        private async Task<bool> HandleAppUserQuestionnaireAndQuestionsAnswered(AppUser appUser)
        {
            if (appUserQuestionnaireService == null || appUserAnswerService == null)
            {
                await Shell.Current.GoToAsync("///ErrorView");
                return false;
            }

            // If the user has not completed the questionnaire, navigate to QuestionnaireView
            var userQuestionnaire = await appUserQuestionnaireService.GetUserQuestionnaireByUserIdAsync(appUser.Id);
            if (userQuestionnaire == null)
            {
                await Shell.Current.GoToAsync("///AppUserQuestionnaireView");
                return false;
            }

            return true;
        }

        private async Task InsertUserToDatabase(AppUser appUser)
        {
            try
            {
                if (databaseService == null)
                {
                    await Shell.Current.GoToAsync("///ErrorView");
                    return;
                }

                await databaseService.InsertAppUserAsync(appUser);
                Debug.WriteLine("User saved to database successfully.");

                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving user to database: {ex.Message}");
                await Shell.Current.GoToAsync("///ErrorView");
            }
        }
    }
}
