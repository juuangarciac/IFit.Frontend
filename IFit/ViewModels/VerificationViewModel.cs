using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos;
using IFit.Models.Dtos.Auth;
using IFit.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IFit.ViewModels;

public partial class VerificationViewModel : ObservableObject
{
    #region Fields
    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(VerifyEmailCommand))]
    public partial string VerificationCode { get; set; } = string.Empty;

    #endregion

    #region Services

    private readonly AuthenticationService authenticationService;

    private readonly DatabaseService _databaseService;

    #endregion

    #region Enums

    public enum RegistrationState
    {
        Idle,
        Verifying,
        Verified,
        Error
    }

    #endregion

    #region Properties

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

    #region Constructor 
    /// <summary>
    /// Consutrctor con inyección de dependencias
    /// </summary>

    public VerificationViewModel(AuthenticationService authenticationService, DatabaseService databaseService)
    {
        this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _databaseService = databaseService;
    }

    /// <summary>
    /// Constructor sin parámetros para compatibilidad con XAML
    /// </summary>
    public VerificationViewModel() : this(
        App.GetService<AuthenticationService>() ?? throw new InvalidOperationException("AuthenticationService no registrado"),
        App.GetService<DatabaseService>() ?? throw new InvalidOperationException("DatabaseService no registrado"))
        {
        // Cargar email desde preferencias
        Email = Preferences.Get("UserEmail", string.Empty);
        Console.WriteLine("UserEmail: " + Email + " found.");
    }

    #endregion

    #region Commands

    /// <summary>
    /// Verifica el correo electrónico del usuario
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanVerifyEmail))]
    public async Task VerifyEmailAsync()
    {
        CurrentState = RegistrationState.Verifying;
        try
        {
            //  Obtener password de forma async 
            string password = await SecureStorage.GetAsync("UserPassword") ?? string.Empty;

            if (string.IsNullOrEmpty(password))
            {
                ErrorMessage = "No se encontró la contraseña almacenada.";
                return;
            }

            //  Usar la tupla para obtener el resultado
            var (success, authData, errorMessage) = await authenticationService.VerifyEmailAsync(
                Email,
                VerificationCode,
                password
            );

            if (success && authData != null)
            {
                // Verificación exitosa
                Console.WriteLine($"Email verificado correctamente para: {Email}");

                // Limpiar credenciales temporales
                SecureStorage.Remove("UserPassword");

                // Guardar datos del usuario
                await InsertUserToDatabase(authData.AppUser);

                // Navegar a la pantalla ExperienceLevelSelectionView
                await Shell.Current.GoToAsync("///GetStartedView");
            }
            else
            {
                //  Error en la verificación
                ErrorMessage = errorMessage ?? "Código de verificación inválido";

                await Application.Current.MainPage.DisplayAlert(
                    "Error de verificación",
                    ErrorMessage,
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            CurrentState = RegistrationState.Error;
            ErrorMessage = "Error de conexión. Por favor, intenta de nuevo.";
            Debug.WriteLine($" Excepción en VerifyEmailAsync: {ex.Message}");

            await Application.Current.MainPage.DisplayAlert(
                "Error",
                ErrorMessage,
                "OK"
            );
        }
        finally
        {
            CurrentState = RegistrationState.Verified;
        }
    }

    private Boolean CanVerifyEmail()
    {
        return !string.IsNullOrWhiteSpace(VerificationCode);
    }

    #endregion

    #region Private Methods

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

            // No lanzamos excepción porque el login ya fue exitoso
            // La BD local es solo para caché
        }
    }

    #endregion
}