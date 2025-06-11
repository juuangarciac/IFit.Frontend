
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
        public Validatable<string> Name { get; set; } = new Validatable<string>();
        public Validatable<string> Email { get; set; } = new Validatable<string>();
        public Validatable<string> Password { get; set; } = new Validatable<string>();

        public AppUserService appUserService;

        public ICommand RegisterCommand { get; }

        public SignUpViewModel()
        {
            appUserService = new AppUserService();
            RegisterCommand = new Command(saveAppUserCredentials);
        }

        public async void saveAppUserCredentials()
        {
            // Validate the user input
            this.AddValidations();
            this.Validate();

            if (await EmailAlreadyExists(Email.Value))
            {
                if (App.Current?.MainPage != null) 
                {
                    await App.Current.MainPage.DisplayAlert("Error", "El correo electrónico ya está en uso.", "OK");
                }
                return;
            }

            // Save in Preferences User credentials
            Preferences.Set("UserName", Name.Value);
            Preferences.Set("UserEmail", Email.Value);
            Preferences.Set("UserPassword", Password.Value);

            Console.WriteLine("Username: " + Name + ", UserEmail: " + Email + " saved.");

            if (Shell.Current != null) 
            {
                await Shell.Current.GoToAsync("///VerificationView");
            }
        }

        private void Validate()
        {
            // Validate the Name, Email, and Password fields
            if (!Name.Validate() || !Email.Validate() || !Password.Validate())
            {
                // If validation fails, show an alert with the validation messages
                var validationMessages = new StringBuilder();
                foreach (var error in Name.Errors.Concat(Email.Errors).Concat(Password.Errors))
                {
                    validationMessages.AppendLine(error);
                }

                Application.Current?.MainPage?.DisplayAlert("Error de validación", validationMessages.ToString(), "OK");
            }
        }

        private void AddValidations()
        {
            // IsNotNullOrEmpty validation for Name, Email, and Password
            Name.Validations.Add(new IsNotNullOrEmptyRule<string> { ValidationMessage = "El nombre es obligatorio." });
            Email.Validations.Add(new IsNotNullOrEmptyRule<string> { ValidationMessage = "El correo electrónico es obligatorio." });
            Password.Validations.Add(new IsNotNullOrEmptyRule<string> { ValidationMessage = "La contraseña es obligatoria." });

            // Email validation
            Email.Validations.Add(new IsValidEmailRule<string> { ValidationMessage = "El correo electrónico no es válido." });

            // Password validation
            Password.Validations.Add(new IsValidPasswordRule<string> { ValidationMessage = "La contraseña debe tener al menos 8 caracteres e incluir: una letra mayúscula, una letra minúscula, un número y un símbolo especial (como @, #, $, %, etc.)." });
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
