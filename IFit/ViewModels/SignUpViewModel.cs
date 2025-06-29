
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

        public AppUserService appUserService;
        public AuthenticationService authenticationService;

        public ICommand RegisterCommand { get; }

        public SignUpViewModel()
        {
            appUserService = new AppUserService();
            authenticationService = new AuthenticationService();

            RegisterCommand = new Command(CreateAccount);
        }

        public async void CreateAccount()
        {
            Console.WriteLine("Username: " + Name + ", UserEmail: " + Email);

            // Setter values
            ValidatableName.Value = Name;
            ValidatableEmail.Value = Email;
            ValidatablePassword.Value = Password;

            // Validate the user input
            this.AddValidations();

            StringBuilder validationMessages = this.Validate();
            if (!string.IsNullOrEmpty(validationMessages.ToString()))
            {
                Application.Current?.MainPage?.DisplayAlert("Error de validación", validationMessages.ToString(), "OK");
                return;
            }

            if (await EmailAlreadyExists(ValidatableEmail.Value))
            {
                if (App.Current?.MainPage != null) 
                {
                    await App.Current.MainPage.DisplayAlert("Error", "El correo electrónico ya está en uso.", "OK");
                }
                return;
            }

            // Save in Preferences User credentials
            Preferences.Set("UserName", ValidatableName.Value);
            Preferences.Set("UserEmail", ValidatableEmail.Value);
            Preferences.Set("UserPassword", ValidatablePassword.Value);

            await authenticationService.SignUpAsync(ValidatableName.Value, ValidatableEmail.Value, ValidatablePassword.Value);

            Console.WriteLine("Username: " + ValidatableName.Value + ", UserEmail: " + ValidatableEmail.Value + " saved.");

            await authenticationService.SendVerificationEmail(Email);

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

        private async Task<Boolean> EmailAlreadyExists(string email)
        {
            var validationResponse = await appUserService.findUserByEmail(email);
            if (validationResponse != null && validationResponse.isPresent())
            {
                return true; // Email already exists
            }
            return false;
        }
    }
}
