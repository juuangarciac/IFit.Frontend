
using IFit.Models;
using IFit.Models.Dtos;
using IFit.Services;
using IFit.Validations.Rules;
using Plugin.ValidationRules;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IFit.ViewModels
{
    class SignUpViewModel
    {
        public String Name { get; set; } = string.Empty;
        public String Email { get; set; }  = string.Empty;
        public String Password { get; set; } = string.Empty;

        public Validatable<string> ValidatableName { get; set; } = new Validatable<string>();
        public Validatable<string> ValidatableEmail { get; set; } = new Validatable<string>();
        public Validatable<string> ValidatablePassword { get; set; } = new Validatable<string>();
            
        public AppUserService? appUserService = App.GetService<AppUserService>();
        public AuthenticationService? authenticationService = App.GetService<AuthenticationService>();
        public DatabaseService? databaseService = App.GetService<DatabaseService>();

        public ICommand RegisterCommand { get; }

        public SignUpViewModel()
        {
            RegisterCommand = new Command(CreateAccount);
        }

        public async void CreateAccount()
        {
            Console.WriteLine("Username: " + Name + ", UserEmail: " + Email);

            // Check if services are initialized
            if (appUserService == null || authenticationService == null || databaseService == null)
            {
                if (App.Current?.MainPage != null)
                {
                    await App.Current.MainPage.DisplayAlert("Error", "Los servicios no están inicializados. Por favor, intente nuevamente más tarde.", "OK");
                    await Shell.Current.GoToAsync("//ErrorView");
                }
                return;
            }

            // Setter values
            ValidatableName.Value = Name;
            ValidatableEmail.Value = Email;
            ValidatablePassword.Value = Password;

            // Validate the user input
            this.AddValidations();
            string validationMessages = this.Validate().ToString();

            if (!string.IsNullOrEmpty(validationMessages))
            {
                if (App.Current?.MainPage != null)
                {
                    await App.Current.MainPage.DisplayAlert("Error", validationMessages, "OK");
                }
                return;
            }
            
            AppUser? user = await authenticationService.SignUpAsync(ValidatableName.Value, ValidatableEmail.Value, ValidatablePassword.Value);
            if(user == null)
            {
                if (App.Current?.MainPage != null)
                {
                    await App.Current.MainPage.DisplayAlert("Error", "No se pudo crear la cuenta. Por favor, intente nuevamente.", "OK");
                }
                return;
            }
            
            EmailValidationResponseDto? emailValidationResponse = await authenticationService.SendVerificationEmail(Email);
            if (emailValidationResponse == null)
            {
                if (App.Current?.MainPage != null)
                {
                    await App.Current.MainPage.DisplayAlert("Error", "No se pudo enviar el correo de verificación. Por favor, intente nuevamente.", "OK");
                }
                return;
            }

            Preferences.Set("UserEmail", Email); // Store the email for later use
            Preferences.Set("UserName", Name); // Store the name for later use

            await databaseService.SaveAppUserAsync(user);
            await Shell.Current.GoToAsync("///VerificationView");
        }

        private StringBuilder Validate()
        {
            // Validate the Name, Email, and Password fields
            var validationMessages = new StringBuilder();

            if (!ValidatableName.Validate() || !ValidatableEmail.Validate() || !ValidatablePassword.Validate())
            {
                // If validation fails, show an alert with the validation messages
                foreach (var error in ValidatableName.Errors.Concat(ValidatableEmail.Errors).Concat(ValidatablePassword.Errors))
                {
                    validationMessages.AppendLine(error);
                }
            }
            return validationMessages;
        }

        private void AddValidations()
        {
            // IsNotNullOrEmpty validation for Name, Email, and Password
            ValidatableName.Validations.Add(new IsNotNullOrEmptyRule<string> { ValidationMessage = "El nombre es obligatorio." });
            ValidatableEmail.Validations.Add(new IsNotNullOrEmptyRule<string> { ValidationMessage = "El correo electrónico es obligatorio." });
            ValidatablePassword.Validations.Add(new IsNotNullOrEmptyRule<string> { ValidationMessage = "La contraseña es obligatoria." });

            // Email validation
            ValidatableEmail.Validations.Add(new IsValidEmailRule<string> { ValidationMessage = "El correo electrónico no es válido." });

            // Password validation
            ValidatablePassword.Validations.Add(new IsValidPasswordRule<string> { ValidationMessage = "La contraseña debe tener al menos 8 caracteres e incluir: una letra mayúscula, una letra minúscula, un número y un símbolo especial (como @, #, $, %, etc.)." });
        }
    }
}
