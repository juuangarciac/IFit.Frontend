using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos.Auth;
using IFit.Services;
using IFit.Validations.Rules;
using Plugin.ValidationRules;
using System.Diagnostics;
using System.Text;

namespace IFit.ViewModels
{
    /// <summary>
    /// ViewModel para la página de registro de nuevos usuarios.
    /// Maneja validación de formulario, registro y navegación post-registro.
    /// </summary>
    public partial class SignUpViewModel : ObservableObject
    {
        #region Services (Inyección de Dependencias)

        private readonly AppUserService _appUserService;
        private readonly AuthenticationService _authenticationService;
        private readonly DatabaseService _databaseService;

        #endregion

        #region State Enum

        public enum RegistrationState
        {
            Idle,           // Estado inicial
            Validating,     // Validando inputs
            Registering,    // Proceso de registro en curso
            Success,        // Registro exitoso
            Error           // Error en el proceso
        }

        #endregion

        #region Observable Properties

        /// <summary>
        /// Nombre del usuario
        /// </summary>
        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        /// <summary>
        /// Email del usuario
        /// </summary>
        [ObservableProperty]
        public partial string Email { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña del usuario
        /// </summary>
        [ObservableProperty]
        public partial string Password { get; set; } = string.Empty;

        /// <summary>
        /// Estado actual del proceso de registro
        /// </summary>
        [ObservableProperty]
        public partial RegistrationState CurrentState { get; set; } = RegistrationState.Idle;

        /// <summary>
        /// Mensaje de error para mostrar al usuario
        /// </summary>
        [ObservableProperty]
        public partial string ErrorMessage { get; set; } = string.Empty;

        #endregion

        #region Validatables (Sistema de validación existente)

        public Validatable<string> ValidatableName { get; set; } = new Validatable<string>();
        public Validatable<string> ValidatableEmail { get; set; } = new Validatable<string>();
        public Validatable<string> ValidatablePassword { get; set; } = new Validatable<string>();

        #endregion

        #region Computed Properties

        /// <summary>
        /// Indica si se está procesando el registro
        /// </summary>
        public bool IsLoading => CurrentState == RegistrationState.Registering || CurrentState == RegistrationState.Validating;

        /// <summary>
        /// Indica si hay un error activo
        /// </summary>
        public bool HasError => CurrentState == RegistrationState.Error;

        /// <summary>
        /// Indica si está en estado idle
        /// </summary>
        public bool IsIdle => CurrentState == RegistrationState.Idle;

        /// <summary>
        /// Mensaje de estado para la UI
        /// </summary>
        public string StatusMessage => CurrentState switch
        {
            RegistrationState.Validating => "Validando datos...",
            RegistrationState.Registering => "Creando cuenta...",
            _ => string.Empty
        };

        /// <summary>
        /// Texto del botón de registro
        /// </summary>
        public string RegisterButtonText => IsLoading ? "Registrando..." : "Crear Cuenta";

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor principal con inyección de dependencias
        /// </summary>
        public SignUpViewModel(
            AppUserService appUserService,
            AuthenticationService authenticationService,
            DatabaseService databaseService)
        {
            _appUserService = appUserService ?? throw new ArgumentNullException(nameof(appUserService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            // Inicializar reglas de validación
            InitializeValidations();
        }

        /// <summary>
        /// Constructor sin parámetros para compatibilidad con XAML
        /// </summary>
        public SignUpViewModel() : this(
            App.GetService<AppUserService>() ?? throw new InvalidOperationException("AppUserService no registrado"),
            App.GetService<AuthenticationService>() ?? throw new InvalidOperationException("AuthenticationService no registrado"),
            App.GetService<DatabaseService>() ?? throw new InvalidOperationException("DatabaseService no registrado"))
        {
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            Name = "usertest" + DateTime.Now.Ticks;
            Email = Name + "@test.com";
            Password = "Test1234!";
        }

        #endregion

        #region Property Changed Handlers

        /// <summary>
        /// Cuando el estado cambia, notificar propiedades computadas
        /// </summary>
        partial void OnCurrentStateChanged(RegistrationState value)
        {
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(IsIdle));
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(RegisterButtonText));

            // Notificar que CanExecute del comando cambió
            RegisterCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Cuando el nombre cambia, limpiar errores y actualizar validación
        /// </summary>
        partial void OnNameChanged(string value)
        {
            if (HasError)
            {
                CurrentState = RegistrationState.Idle;
                ErrorMessage = string.Empty;
            }
            ValidatableName.Value = value;
            RegisterCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Cuando el email cambia, limpiar errores y actualizar validación
        /// </summary>
        partial void OnEmailChanged(string value)
        {
            if (HasError)
            {
                CurrentState = RegistrationState.Idle;
                ErrorMessage = string.Empty;
            }
            ValidatableEmail.Value = value;
            RegisterCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Cuando la contraseña cambia, limpiar errores y actualizar validación
        /// </summary>
        partial void OnPasswordChanged(string value)
        {
            if (HasError)
            {
                CurrentState = RegistrationState.Idle;
                ErrorMessage = string.Empty;
            }
            ValidatablePassword.Value = value;
            RegisterCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Commands

        /// <summary>
        /// Comando para crear una nueva cuenta
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRegister))]
        private async Task RegisterAsync()
        {
            try
            {
                CurrentState = RegistrationState.Validating;
                ErrorMessage = string.Empty;

                Debug.WriteLine($"Iniciando registro para: {Name} ({Email})");

                // 1. Validar inputs
                string validationError = ValidateInputs();
                if (!string.IsNullOrEmpty(validationError))
                {
                    CurrentState = RegistrationState.Error;
                    ErrorMessage = validationError;

                    Debug.WriteLine($"Validación fallida: {validationError}");

                    return;
                }

                // 2. Cambiar estado a Registering
                CurrentState = RegistrationState.Registering;

                // 3. Intentar registrar
                var response = await TryRegisterAsync();
                if (response == null)
                {
                    return;
                }

                Debug.WriteLine($"Registro exitoso para usuario: {response.Email}");

                // 4. Guardar datos de sesiónd
                await SaveRegistrationData(Name, Email);

                // 5. Navegar primero (overlay sigue visible durante la creación de VerificationView)
                //    y marcar Success después para evitar el flash de pantalla en blanco.
                Debug.WriteLine("Navegando a VerificationView");
                await Shell.Current.GoToAsync("//VerificationView", new Dictionary<string, object>
                {
                    { "Email", Email }
                });
                CurrentState = RegistrationState.Success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inesperado en RegisterAsync: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                CurrentState = RegistrationState.Error;
                ErrorMessage = "Ocurrió un error inesperado. Por favor, intenta de nuevo.";
            }
        }

        /// <summary>
        /// Determina si el comando Register puede ejecutarse
        /// </summary>
        private bool CanRegister()
        {
            return !string.IsNullOrWhiteSpace(Name)
                   && !string.IsNullOrWhiteSpace(Email)
                   && !string.IsNullOrWhiteSpace(Password);
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Inicializa las reglas de validación
        /// </summary>
        private void InitializeValidations()
        {
            // Validaciones para Name
            ValidatableName.Validations.Add(new IsNotNullOrEmptyRule<string>
            {
                ValidationMessage = "El nombre es obligatorio."
            });

            // Validaciones para Email
            ValidatableEmail.Validations.Add(new IsNotNullOrEmptyRule<string>
            {
                ValidationMessage = "El correo electrónico es obligatorio."
            });
            ValidatableEmail.Validations.Add(new IsValidEmailRule<string>
            {
                ValidationMessage = "El correo electrónico no es válido."
            });

            // Validaciones para Password
            ValidatablePassword.Validations.Add(new IsNotNullOrEmptyRule<string>
            {
                ValidationMessage = "La contraseña es obligatoria."
            });
            ValidatablePassword.Validations.Add(new IsValidPasswordRule<string>
            {
                ValidationMessage = "La contraseña debe tener al menos 8 caracteres e incluir: una letra mayúscula, una letra minúscula, un número y un símbolo especial (como @, #, $, %, etc.)."
            });
        }

        /// <summary>
        /// Valida todos los campos del formulario
        /// </summary>
        private string ValidateInputs()
        {
            // Asignar valores actuales
            ValidatableName.Value = Name;
            ValidatableEmail.Value = Email;
            ValidatablePassword.Value = Password;

            var validationMessages = new StringBuilder();

            // Validar cada campo
            if (!ValidatableName.Validate() || !ValidatableEmail.Validate() || !ValidatablePassword.Validate())
            {
                foreach (var error in ValidatableName.Errors.Concat(ValidatableEmail.Errors).Concat(ValidatablePassword.Errors))
                {
                    validationMessages.AppendLine(error);
                }
            }

            return validationMessages.ToString().TrimEnd();
        }

        #endregion

        #region Registration Methods

        /// <summary>
        /// Intenta registrar al usuario con los datos proporcionados
        /// </summary>
        private async Task<RegisterResponseDto?> TryRegisterAsync()
        {
            try
            {
                Debug.WriteLine($"Intentando registrar usuario: {Email}");

                var response = await _authenticationService.RegisterAsync(
                    ValidatableName.Value,
                    ValidatableEmail.Value,
                    ValidatablePassword.Value
                );

                if (response == null)
                {
                    CurrentState = RegistrationState.Error;
                    ErrorMessage = "No se pudo crear la cuenta. Por favor, inténtalo más tarde.";

                    Debug.WriteLine("Registro fallido: Respuesta nula del servidor");

                    return null;
                }

                if(response.Success == false)
                {
                    CurrentState = RegistrationState.Error;
                    ErrorMessage = response.Message ?? "Error desconocido durante el registro.";
                    Debug.WriteLine($"Registro fallido: {response.Message}");
                    return null;
                }

                Debug.WriteLine($"Registro exitoso. Email: {response.Email}");

                return response;
            }
            catch (Exception ex)
            {
                CurrentState = RegistrationState.Error;
                ErrorMessage = "Error de conexión. Por favor, verifica tu conexión a internet.";

                Debug.WriteLine($"Excepción en TryRegisterAsync: {ex.Message}");

                return null;
            }
        }

        /// <summary>
        /// Guarda los datos del usuario registrado en Preferences y base de datos local
        /// </summary>
        private async Task SaveRegistrationData(string name, string email)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
                {
                    Debug.WriteLine("No se puede guardar: Nombre o Email son nulos o vacíos");
                    return;
                }

                Debug.WriteLine($"Guardando datos de registro para usuario: {Email}");

                // Guardar en Preferences
                Preferences.Set("UserEmail", email);
                Preferences.Set("UserName", name);

                // Almacenar password en SecureStorage para validacion y login automatico en VerificationViewModel
                // (Nota: Se eliminará después de la verificación)
                await SecureStorage.SetAsync("UserPassword", Password);

                Debug.WriteLine("Datos guardados en Preferences");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error guardando datos de registro: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Limpia los campos del formulario
        /// </summary>
        public void ClearForm()
        {
            Name = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            CurrentState = RegistrationState.Idle;
            ErrorMessage = string.Empty;

            // Limpiar validaciones
            ValidatableName.Value = string.Empty;
            ValidatableEmail.Value = string.Empty;
            ValidatablePassword.Value = string.Empty;
            ValidatableName.Errors.Clear();
            ValidatableEmail.Errors.Clear();
            ValidatablePassword.Errors.Clear();

            Debug.WriteLine("Formulario limpiado");
        }

        /// <summary>
        /// Limpia solo los errores
        /// </summary>
        public void ClearErrors()
        {
            CurrentState = RegistrationState.Idle;
            ErrorMessage = string.Empty;

            ValidatableName.Errors.Clear();
            ValidatableEmail.Errors.Clear();
            ValidatablePassword.Errors.Clear();
        }

        #endregion
    }
}