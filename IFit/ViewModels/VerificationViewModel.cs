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

public partial class VerificationViewModel : ObservableObject, IQueryAttributable
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
    [NotifyCanExecuteChangedFor(nameof(VerifyEmailCommand))]
    public partial RegistrationState CurrentState { get; set; } = RegistrationState.Idle;

    /// <summary>
    /// Mensaje de error para mostrar al usuario
    /// </summary>
    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool IsVerifying => CurrentState == RegistrationState.Verifying
                             || CurrentState == RegistrationState.Verified;

    partial void OnCurrentStateChanged(RegistrationState value)
    {
        OnPropertyChanged(nameof(IsVerifying));
    }

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
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ClearVerificationData();

        if (query.TryGetValue("Email", out var emailObj) && emailObj is string email && !string.IsNullOrEmpty(email))
            Email = email;
        else
            Email = Preferences.Get("UserEmail", string.Empty);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Limpiar datos antes de cerrar la pestaña de verificación y volver a la página principal
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    public async Task CloseAsync()
    {
        ClearVerificationData();
        await Shell.Current.GoToAsync("//MainPage");
    }

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
                CurrentState = RegistrationState.Error;
                ErrorMessage = "No se encontró la contraseña almacenada.";
                await NotificationService.ShowErrorAsync(ErrorMessage);
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

                SecureStorage.Remove("UserPassword");

                Preferences.Set("UserId", authData.AppUser.Id);
                await InsertUserToDatabase(authData.AppUser);

                CurrentState = RegistrationState.Verified;
                await Shell.Current.GoToAsync("//GetStartedView");
            }
            else
            {
                CurrentState = RegistrationState.Error;
                ErrorMessage = errorMessage ?? "Código de verificación inválido";
                await NotificationService.ShowErrorAsync(ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            CurrentState = RegistrationState.Error;
            ErrorMessage = "Error de conexión. Por favor, intenta de nuevo.";
            Debug.WriteLine($" Excepción en VerifyEmailAsync: {ex.Message}");

            await NotificationService.ShowErrorAsync(ErrorMessage);
        }
        finally
        {
            ClearVerificationData();
        }
    }

    private bool CanVerifyEmail()
    {
        return !string.IsNullOrWhiteSpace(VerificationCode) && CurrentState != RegistrationState.Verifying;
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

    /// <summary>
    /// Limpia los datos de verificación del usuario en la base de datos local SQLite después de un registro exitoso
    /// </summary>
    /// <param name="appUser"></param>
    /// <returns></returns>
    private void ClearVerificationData()
    {
        VerificationCode = string.Empty;
        CurrentState = RegistrationState.Idle;
        ErrorMessage = string.Empty;
    }

    #endregion
}