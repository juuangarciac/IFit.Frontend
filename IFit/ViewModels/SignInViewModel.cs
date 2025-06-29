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
            LoginCommand = new Command(async () => await authenticationService.LoginAsync(Email, Password));
        }

    }
}
