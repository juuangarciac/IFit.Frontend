using IFit;
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

        private readonly AuthenticationService authenticationService;
        private readonly AppUserService appUserService;
        private DatabaseService? databaseService;

        public ICommand LoginCommand { get; }

        public SignInViewModel()
        {
            authenticationService = new AuthenticationService();
            appUserService = new AppUserService();
            databaseService = App.GetService<DatabaseService>();

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

            if (!await HandleVerificationAsync(appUser)) return;

            await HandleCoachSelectionAsync(appUser);
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ShowError("Ups!", "Por favor, ingrese su correo electrónico y contraseña.");
                return false;
            }
            return true;
        }

        private async Task<SignInResponseDto?> TryLoginAsync()
        {
            var response = await authenticationService.LoginAsync(Email, Password);
            if (response == null)
            {
                ShowError("Ups!", "No se pudo iniciar sesión. Por favor, verifique sus credenciales.");
            }
            return response;
        }

        private void SaveLoginData(SignInResponseDto dto)
        {
            Preferences.Set("UserEmail", Email);
            Preferences.Set("UserToken", dto.token);
            Preferences.Set("UserAuthorities", dto.authorities);
        }

        private async Task<AppUser?> LoadUserDataAsync()
        {
            var user = await appUserService.findUserByEmail(Email);
            if (user == null)
            {
                ShowError("Ups!", "No se pudo recuperar la información del usuario. Por favor, intente nuevamente.");
                await Shell.Current.GoToAsync("///ErrorView");
            }
            Preferences.Set("UserId", user?.Id ?? 0);

            return user;
        }

        private async Task<bool> HandleVerificationAsync(AppUser appUser)
        {
            if (!appUser.IsVerified)
            {
                // ShowError("Parece que se le olvido algo la última vez!", "Por favor, verifique su correo electrónico antes de continuar.");
                await authenticationService.SendVerificationEmail(Email);
                await Shell.Current.GoToAsync("///VerificationView");
                return false;
            }

            Preferences.Set("IsVerified", appUser.IsVerified);
            return true;
        }

        private async Task HandleCoachSelectionAsync(AppUser appUser)
        {
            if (string.IsNullOrEmpty(appUser.CoachModelTypeId))
            {
                // ShowError("Parece que se le olvido algo la última vez!", "Por favor, seleccione un tipo de modelo de entrenador antes de continuar.");
                await Shell.Current.GoToAsync("///GetStartedView");
                return;
            }

            Preferences.Set("CoachModelTypeId", appUser.CoachModelTypeId);
            await Shell.Current.GoToAsync("///HomeView");
        }

        private async void ShowError(string header, string message)
        {
            if (App.Current?.MainPage != null)
            {
                await App.Current.MainPage.DisplayAlert(header, message, "OK");
            }
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
