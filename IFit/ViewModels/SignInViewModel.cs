using IFit;
using IFit.Models.Dtos;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using IFit.Services;

namespace IFit.ViewModels
{
    public class SignInViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        private AuthenticationService authenticationService;

        public ICommand LoginCommand { get; }

        public SignInViewModel()
        {
            // Initialize the AuthenticationService instance
            authenticationService = new AuthenticationService();
            LoginCommand = new Command(SignIn);
        }

        public async void SignIn()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                if(App.Current?.MainPage != null)
                {
                    await App.Current.MainPage.DisplayAlert("Error", "Por favor, ingrese su correo electrónico y contraseña.", "OK");
                }
                return;
            }

            SignInResponseDto signInResponseDto = await authenticationService.LoginAsync(Email, Password);
            
            if(signInResponseDto == null)
            {
                if(App.Current?.MainPage != null)
                {
                    await App.Current.MainPage.DisplayAlert("Error", "No se pudo iniciar sesión. Por favor, verifique sus credenciales.", "OK");
                }
                return;
            }

            Preferences.Set("UserEmail", Email);
            Preferences.Set("UserToken", signInResponseDto.token);
            Preferences.Set("UserAuthorities", signInResponseDto.authorities);
            Console.WriteLine("UserEmail: " + Email + ", UserToken: " + signInResponseDto.token + " saved.");

            await Shell.Current.GoToAsync("///HomeView");
        }

    }
}
