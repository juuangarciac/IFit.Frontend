using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Helper;
using IFit.Models.Dtos.Auth;
using IFit.Services;
using System.Diagnostics;

namespace IFit.ViewModels
{
    /// <summary>
    /// ViewModel para la p�gina de inicio de sesi�n.
    /// Maneja autenticaci�n, validaci�n y navegaci�n post-login.
    /// </summary>
    public partial class SignInViewModel : ObservableObject
    {
        #region Services (Inyecci�n de Dependencias)

        private readonly AuthenticationService _authenticationService;
        private readonly AppUserService _appUserService;
        private readonly DatabaseService _databaseService;

        #endregion

        #region State Enum

        public enum LoginState
        {
            Idle,           // Estado inicial
            Loading,        // Proceso de login en curso
            Success,        // Login exitoso
            Error           // Error en el proceso
        }

        #endregion

        #region Observable Properties

        /// <summary>
        /// Email del usuario
        /// </summary>
        [ObservableProperty]
        public partial string Email { get; set; } = string.Empty;

        /// <summary>
        /// Contrase�a del usuario
        /// </summary>
        [ObservableProperty]
        public partial string Password { get; set; } = string.Empty;

        /// <summary>
        /// Estado actual del proceso de login
        /// </summary>
        [ObservableProperty]
        public partial LoginState CurrentState { get; set; } = LoginState.Idle;

        /// <summary>
        /// Mensaje de error para mostrar al usuario
        /// </summary>
        [ObservableProperty]
        public partial string ErrorMessage { get; set; } = string.Empty;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Indica si se est� procesando el login
        /// </summary>
        public bool IsLoading => CurrentState == LoginState.Loading;

        /// <summary>
        /// Indica si hay un error activo
        /// </summary>
        public bool HasError => CurrentState == LoginState.Error;

        /// <summary>
        /// Indica si est� en estado idle (inicial)
        /// </summary>
        public bool IsIdle => CurrentState == LoginState.Idle;

        /// <summary>
        /// Mensaje de estado para la UI
        /// </summary>
        public string StatusMessage => IsLoading ? "Iniciando sesi�n..." : string.Empty;

        /// <summary>
        /// Texto del bot�n de login
        /// </summary>
        public string LoginButtonText => IsLoading ? "Iniciando..." : "Iniciar Sesi�n";

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor principal con inyecci�n de dependencias
        /// </summary>
        public SignInViewModel(
            AuthenticationService authenticationService,
            AppUserService appUserService,
            DatabaseService databaseService)
        {
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _appUserService = appUserService ?? throw new ArgumentNullException(nameof(appUserService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// Constructor sin par�metros para compatibilidad con XAML
        /// </summary>
        public SignInViewModel() : this(
            App.GetService<AuthenticationService>() ?? throw new InvalidOperationException("AuthenticationService no registrado"),
            App.GetService<AppUserService>() ?? throw new InvalidOperationException("AppUserService no registrado"),
            App.GetService<DatabaseService>() ?? throw new InvalidOperationException("DatabaseService no registrado"))
        {
        }

        #endregion

        #region Property Changed Handlers

        /// <summary>
        /// Cuando el estado cambia, notificar propiedades computadas
        /// </summary>
        partial void OnCurrentStateChanged(LoginState value)
        {
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(IsIdle));
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(LoginButtonText));

            // Notificar que CanExecute del comando cambi�
            SignInCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Cuando el email cambia, limpiar errores y actualizar comando
        /// </summary>
        partial void OnEmailChanged(string value)
        {
            if (HasError)
            {
                CurrentState = LoginState.Idle;
                ErrorMessage = string.Empty;
            }
            SignInCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Cuando la contrase�a cambia, limpiar errores y actualizar comando
        /// </summary>
        partial void OnPasswordChanged(string value)
        {
            if (HasError)
            {
                CurrentState = LoginState.Idle;
                ErrorMessage = string.Empty;
            }
            SignInCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Commands

        /// <summary>
        /// Comando para iniciar sesi�n con validaci�n de CanExecute
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSignIn))]
        private async Task SignInAsync()
        {
            try
            {
                CurrentState = LoginState.Loading;
                ErrorMessage = string.Empty;

                Debug.WriteLine("Iniciando proceso de login");

                // 1. Validar inputs
                if (!ValidateInputs())
                {
                    return;
                }

                // 2. Intentar login
                var appUser = await TryLoginAsync();
                if (appUser == null)
                {
                    return;
                }

                Debug.WriteLine($"Login exitoso para usuario: {appUser.Email}");

                // 3. Guardar datos de sesi�n
                await SaveLoginData(appUser);

                // 4. Verificar si necesita completar el proceso de registro
                if (!appUser.RegistrationComplete)
                {
                    Debug.WriteLine("Usuario no ha completado registro inicial");

                    // 4a. Verificar email primero
                    if (!await HandleVerificationAsync(appUser))
                    {
                        return;
                    }

                    // 4b. Luego seleccionar coach y nivel de experiencia
                    if (!await HandleUserCoachAndExperienceLevel(appUser))
                    {
                        return;
                    }
                }

                // 5. Navegar primero (el overlay sigue visible durante la creación de HomeView)
                //    y marcar Success después: así el usuario no ve la pantalla de login
                //    en blanco mientras MAUI infla la vista de destino.
                Debug.WriteLine("Navegando a HomeView");
                await Shell.Current.GoToAsync("///HomeView");
                CurrentState = LoginState.Success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inesperado en SignInAsync: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                CurrentState = LoginState.Error;
                ErrorMessage = "Ocurri� un error inesperado. Por favor, intenta de nuevo.";
            }
        }

        /// <summary>
        /// Determina si el comando SignIn puede ejecutarse
        /// </summary>
        private bool CanSignIn()
        {
            return !string.IsNullOrWhiteSpace(Email)
                   && !string.IsNullOrWhiteSpace(Password)
                   && CurrentState != LoginState.Loading;
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Valida los campos de entrada antes del login
        /// </summary>
        private bool ValidateInputs()
        {
            // Email validation
            if (string.IsNullOrWhiteSpace(Email))
            {
                CurrentState = LoginState.Error;
                ErrorMessage = "Por favor, ingresa tu email.";
                return false;
            }

            if (!IsValidEmail(Email))
            {
                CurrentState = LoginState.Error;
                ErrorMessage = "Por favor, ingresa un email v�lido.";
                return false;
            }

            // Password validation
            if (string.IsNullOrWhiteSpace(Password))
            {
                CurrentState = LoginState.Error;
                ErrorMessage = "Por favor, ingresa tu contrase�a.";
                return false;
            }

            if (Password.Length < 8)
            {
                CurrentState = LoginState.Error;
                ErrorMessage = "La contrase�a debe tener al menos 8 caracteres.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Valida si un email tiene formato correcto
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Authentication Methods

        /// <summary>
        /// Intenta hacer login con las credenciales proporcionadas
        /// </summary>
        private async Task<AppUserResponseDto?> TryLoginAsync()
        {
            try
            {
                Debug.WriteLine($"Intentando login para: {Email}");

                var response = await _authenticationService.LoginAsync(Email, Password);

                if (response == null)
                {
                    CurrentState = LoginState.Error;
                    ErrorMessage = "Credenciales incorrectas. Por favor, verifica tu email y contrase�a.";

                    Debug.WriteLine("Login fallido: Respuesta nula del servidor");
                    return null;
                }

                if (response.AppUser == null)
                {
                    CurrentState = LoginState.Error;
                    ErrorMessage = "Error al obtener datos del usuario.";

                    Debug.WriteLine("Login fallido: AppUser es null");

                    return null;
                }

                Debug.WriteLine($"Login exitoso. UserId: {response.AppUser.Id}");
                return response.AppUser;
            }
            catch (Exception ex)
            {
                CurrentState = LoginState.Error;
                ErrorMessage = "Error de conexi�n. Por favor, verifica tu conexi�n a internet.";

                Debug.WriteLine($"Excepci�n en TryLoginAsync: {ex.Message}");

                return null;
            }
        }

        /// <summary>
        /// Guarda los datos del usuario en Preferences y base de datos local
        /// </summary>
        private async Task SaveLoginData(AppUserResponseDto dto)
        {
            try
            {
                Debug.WriteLine($"Guardando datos de login para usuario: {dto.Email}");

                // Guarda en Preferences para acceso r�pido
                Preferences.Set("UserId", dto.Id);
                Preferences.Set("UserEmail", dto.Email);
                Preferences.Set("Name", dto.Name);
                Preferences.Set("IsVerified", dto.Verified);

                if (!string.IsNullOrEmpty(dto.CoachModelTypeName))
                {
                    Preferences.Set("CoachModelTypeName", dto.CoachModelTypeName);
                }

                Debug.WriteLine("Datos guardados en Preferences");

                // Guardar en base de datos local (fire-and-forget: es solo caché, no bloquea el flujo)
                _ = InsertUserToDatabase(dto);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error guardando datos de login: {ex.Message}");
                // No bloqueamos el flujo si falla el guardado local
                // El usuario ya est� autenticado en el servidor
            }
        }

        /// <summary>
        /// Verifica si el usuario ha confirmado su email
        /// </summary>
        private async Task<bool> HandleVerificationAsync(AppUserResponseDto appUser)
        {
            try
            {
                if (!appUser.Verified)
                {
                    Debug.WriteLine("Usuario no verificado, navegando a VerificationView");

                    // Opcionalmente enviar email de verificaci�n
                    // await _authenticationService.SendVerificationEmail(Email);

                    await Shell.Current.GoToAsync("///VerificationView");
                    return false;
                }

                Debug.WriteLine("Usuario verificado correctamente");
                Preferences.Set("IsVerified", true);
                return true;
            }
            catch (Exception ex)
            {
                CurrentState = LoginState.Error;
                ErrorMessage = "No se pudo verificar el estado del usuario. Por favor, intenta nuevamente.";

                Debug.WriteLine($"Error en HandleVerificationAsync: {ex.Message}");

                return false;
            }
        }

        /// <summary>
        /// Verifica si el usuario ha seleccionado coach y nivel de experiencia
        /// </summary>
        private async Task<bool> HandleUserCoachAndExperienceLevel(AppUserResponseDto appUser)
        {
            try
            {
                var needsCoachSelection = string.IsNullOrEmpty(appUser.CoachModelTypeName);
                var needsExperienceLevel = string.IsNullOrEmpty(appUser.ExperienceLevelName);

                if (needsCoachSelection || needsExperienceLevel)
                {
                    Debug.WriteLine($"Usuario necesita completar setup - Coach: {needsCoachSelection}, Experience: {needsExperienceLevel}");

                    await Shell.Current.GoToAsync("///GetStartedView");
                    return false;
                }

                Debug.WriteLine("Usuario tiene coach y nivel de experiencia configurados");
                Preferences.Set("CoachModelTypeName", appUser.CoachModelTypeName);

                return true;
            }
            catch (Exception ex)
            {
                CurrentState = LoginState.Error;
                ErrorMessage = "No se pudo verificar la configuraci�n del usuario.";

                Debug.WriteLine($"Error en HandleUserCoachAndExperienceLevel: {ex.Message}");

                return false;
            }
        }

        /// <summary>
        /// Guarda el usuario en la base de datos local SQLite
        /// </summary>
        private async Task InsertUserToDatabase(AppUserResponseDto appUser)
        {
            try
            {
                Debug.WriteLine($"Insertando usuario en BD local: {appUser.Email}");

                var entity = appUser.toEntity();
                await _databaseService.InsertAppUserAsync(entity);

                Debug.WriteLine("Usuario guardado en BD local exitosamente");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error guardando usuario en BD local: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // No lanzamos excepci�n porque el login ya fue exitoso
                // La BD local es solo para cach�
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Limpia los campos del formulario
        /// </summary>
        public void ClearForm()
        {
            Email = string.Empty;
            Password = string.Empty;
            CurrentState = LoginState.Idle;
            ErrorMessage = string.Empty;

            Debug.WriteLine("Formulario limpiado");
        }

        /// <summary>
        /// Limpia solo los errores
        /// </summary>
        public void ClearErrors()
        {
            CurrentState = LoginState.Idle;
            ErrorMessage = string.Empty;
        }

        #endregion
    }
}