using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos.Auth;
using IFit.Services;
using IFit.Validations.Rules;
using Plugin.ValidationRules;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

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
        private string _name = string.Empty;

        /// <summary>
        /// Email del usuario
        /// </summary>
        [ObservableProperty]
        private string _email = string.Empty;

        /// <summary>
        /// Contraseña del usuario
        /// </summary>
        [ObservableProperty]
        private string _password = string.Empty;

        /// <summary>
        /// Estado actual del proceso de registro
        /// </summary>
        [ObservableProperty]
        private RegistrationState _currentState = RegistrationState.Idle;

        /// <summary>
        /// Mensaje de error para mostrar al usuario
        /// </summary>
        [ObservableProperty]
        private string _errorMessage = string.Empty;

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

                    if (App.Current?.MainPage != null)
                    {
                        await App.Current.MainPage.DisplayAlert("Error de Validación", validationError, "OK");
                    }
                    return;
                }

                // 2. Intentar registro
                CurrentState = RegistrationState.Registering;
                var response = await TryRegisterAsync();
                if (response == null)
                {
                    return;
                }

                Debug.WriteLine($"Registro exitoso para usuario: {response.AppUser?.Email}");

                // 3. Guardar datos de sesión
                await SaveRegistrationData(response);

                // 4. Marcar como exitoso y navegar
                CurrentState = RegistrationState.Success;
                Debug.WriteLine("Navegando a VerificationView");
                await Shell.Current.GoToAsync("///VerificationView");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inesperado en RegisterAsync: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                CurrentState = RegistrationState.Error;
                ErrorMessage = "Ocurrió un error inesperado. Por favor, intenta de nuevo.";

                if (App.Current?.MainPage != null)
                {
                    await App.Current.MainPage.DisplayAlert(
                        "Error Inesperado",
                        "No se pudo completar el registro. Por favor, intenta nuevamente.",
                        "OK"
                    );
                }
            }
            finally
            {
                // Asegurar que no quede en Loading si algo falla
                if (CurrentState == RegistrationState.Registering || CurrentState == RegistrationState.Validating)
                {
                    CurrentState = RegistrationState.Error;
                }
            }
        }

        /// <summary>
        /// Determina si se puede ejecutar el comando de registro
        /// </summary>
        private bool CanRegister()
        {
            return !IsLoading
                   && !string.IsNullOrWhiteSpace(Name)
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
        private async Task<AuthResponse?> TryRegisterAsync()
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
                    ErrorMessage = "No se pudo crear la cuenta. El email podría estar ya registrado.";

                    Debug.WriteLine("Registro fallido: Respuesta nula del servidor");

                    if (App.Current?.MainPage != null)
                    {
                        await App.Current.MainPage.DisplayAlert(
                            "Error de Registro",
                            "No se pudo crear la cuenta. Por favor, verifica que el email no esté ya registrado.",
                            "OK"
                        );
                    }

                    return null;
                }

                if (response.AppUser == null)
                {
                    CurrentState = RegistrationState.Error;
                    ErrorMessage = "Error al obtener datos del usuario.";

                    Debug.WriteLine("Registro fallido: AppUser es null");

                    if (App.Current?.MainPage != null)
                    {
                        await App.Current.MainPage.DisplayAlert(
                            "Error",
                            "No se pudieron obtener los datos del usuario.",
                            "OK"
                        );
                    }

                    return null;
                }

                Debug.WriteLine($"Registro exitoso. UserId: {response.AppUser.Id}");
                return response;
            }
            catch (Exception ex)
            {
                CurrentState = RegistrationState.Error;
                ErrorMessage = "Error de conexión. Por favor, verifica tu conexión a internet.";

                Debug.WriteLine($"Excepción en TryRegisterAsync: {ex.Message}");

                if (App.Current?.MainPage != null)
                {
                    await App.Current.MainPage.DisplayAlert(
                        "Error de Conexión",
                        "No se pudo conectar con el servidor. Por favor, verifica tu conexión a internet.",
                        "OK"
                    );
                }

                return null;
            }
        }

        /// <summary>
        /// Guarda los datos del usuario registrado en Preferences y base de datos local
        /// </summary>
        private async Task SaveRegistrationData(AuthResponse response)
        {
            try
            {
                if (response.AppUser == null)
                {
                    Debug.WriteLine("No se puede guardar: AppUser es null");
                    return;
                }

                Debug.WriteLine($"Guardando datos de registro para usuario: {response.AppUser.Email}");

                // Guardar en Preferences
                Preferences.Set("UserEmail", Email);
                Preferences.Set("UserName", Name);
                Preferences.Set("UserId", response.AppUser.Id);

                Debug.WriteLine("Datos guardados en Preferences");

                // Guardar en base de datos local
                await _databaseService.SaveAppUserAsync(response.AppUser.toEntity());

                Debug.WriteLine("Usuario guardado en BD local");

                /* Envío de email de verificación (deshabilitado temporalmente)
                var emailValidationResponse = await _authenticationService.SendVerificationEmail(Email);
                if (emailValidationResponse == null)
                {
                    Debug.WriteLine("No se pudo enviar email de verificación");
                }
                */
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error guardando datos de registro: {ex.Message}");
                // No bloqueamos el flujo si falla el guardado local
                // El usuario ya está registrado en el servidor
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