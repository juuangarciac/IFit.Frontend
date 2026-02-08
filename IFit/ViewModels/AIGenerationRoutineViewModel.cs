using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Helper;
using IFit.Models;
using IFit.Models.Dtos.AI;
using IFit.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace IFit.ViewModels;

/// <summary>
/// ViewModel para la generación de rutinas con IA.
/// 
/// FLUJO SIMPLIFICADO:
/// 1. Usuario completa cuestionario → responseId guardado en Preferences
/// 2. Usuario presiona "Generar rutina"
/// 3. Se llama al backend con userId + responseId
/// 4. Backend construye prompt y llama a Ronnie
/// 5. Frontend recibe rutina estructurada
/// 6. Se muestra la rutina al usuario
/// </summary>
public partial class AIGenerationRoutineViewModel : ObservableObject
{
    #region Services
    private readonly AIRoutineService _aiRoutineService;
    private readonly DatabaseService _databaseService;
    #endregion

    #region Fields
    private readonly long _responseId;
    #endregion

    #region Properties

    [ObservableProperty]
    private bool _isGenerating = false;

    [ObservableProperty]
    private bool _isCompleted = false;

    [ObservableProperty]
    private bool _showStartButton = true;

    [ObservableProperty]
    private string _statusMessage = "Presiona el botón para generar tu rutina personalizada";

    [ObservableProperty]
    private RoutineResponseDto? _generatedRoutine;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructor con inyección de dependencias
    /// </summary>
    public AIGenerationRoutineViewModel()
    {
        System.Diagnostics.Debug.WriteLine("=== Iniciando constructor AIGenerationRoutineViewModel ===");

        try
        {
            System.Diagnostics.Debug.WriteLine("→ Obteniendo AIRoutineService...");
            var aiService = App.GetService<AIRoutineService>()
                ?? throw new InvalidOperationException("AIRoutineService no registrado");
            System.Diagnostics.Debug.WriteLine("✓ AIRoutineService obtenido");

            System.Diagnostics.Debug.WriteLine("→ Obteniendo DatabaseService...");
            var dbService = App.GetService<DatabaseService>()
                ?? throw new InvalidOperationException("DatabaseService no registrado");
            System.Diagnostics.Debug.WriteLine("✓ DatabaseService obtenido");

            System.Diagnostics.Debug.WriteLine("→ Obteniendo responseId de Preferences...");
            var responseId = Preferences.Get("responseId", 0L);
            System.Diagnostics.Debug.WriteLine($"✓ ResponseId obtenido: {responseId}");

            if (responseId <= 0)
            {
                System.Diagnostics.Debug.WriteLine("⚠ Warning: responseId inválido o no encontrado");
            }

            _aiRoutineService = aiService;
            _databaseService = dbService;
            _responseId = responseId;

            System.Diagnostics.Debug.WriteLine("=== Constructor AIGenerationRoutineViewModel completado ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"✗ Error en constructor: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Comando para iniciar la generación de la rutina
    /// </summary>
    [RelayCommand]
    private async Task StartGenerationAsync()
    {
        if (IsGenerating) return;

        System.Diagnostics.Debug.WriteLine("=== Iniciando generación de rutina ===");

        ShowStartButton = false;
        IsGenerating = true;
        IsCompleted = false;
        StatusMessage = "Generando tu rutina personalizada...";

        await GenerateRoutineAsync();
    }

    /// <summary>
    /// Comando para navegar a ver la rutina generada
    /// </summary>
    [RelayCommand]
    private async Task NavigateToRoutineAsync()
    {
        if (GeneratedRoutine == null)
        {
            System.Diagnostics.Debug.WriteLine("✗ No hay rutina generada para navegar");
            return;
        }

        try
        {
            // TODO: Navegar a la vista de detalle de rutina
            // Pasar GeneratedRoutine como parámetro de navegación
            await Shell.Current.GoToAsync("//RoutineDetailView");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"✗ Error navegando a rutina: {ex.Message}");
            await ErrorHandler.HandleErrorAsync($"Error al mostrar la rutina: {ex.Message}");
        }
    }

    /// <summary>
    /// Comando para reintentar la generación
    /// </summary>
    [RelayCommand]
    private void RetryGeneration()
    {
        ResetToStart();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Genera la rutina llamando al backend iFit.
    /// El backend se encarga de todo el proceso de construcción del prompt.
    /// </summary>
    private async Task GenerateRoutineAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("→ Obteniendo usuario actual...");

            // Obtener usuario actual
            AppUser? appUser = await _databaseService.GetCurrentUserAsync();
            if (appUser == null)
            {
                System.Diagnostics.Debug.WriteLine("✗ No hay usuario activo");

                await ErrorHandler.HandleErrorAsync(
                    "No hay ningún usuario activo. " +
                    "Por favor, inicia sesión para generar una rutina personalizada."
                );
                ResetToStart();
                return;
            }

            System.Diagnostics.Debug.WriteLine($"✓ Usuario obtenido: {appUser.Name} (ID: {appUser.Id})");

            // Validar responseId
            if (_responseId <= 0)
            {
                System.Diagnostics.Debug.WriteLine("✗ ResponseId inválido");

                await ErrorHandler.HandleErrorAsync(
                    "No se encontró un cuestionario completado. " +
                    "Por favor, completa el cuestionario antes de generar tu rutina."
                );
                ResetToStart();
                return;
            }

            // Convertir el ID de usuario a String (como espera el backend)
            string userId = appUser.Id.ToString();
            System.Diagnostics.Debug.WriteLine($"→ Generando rutina para userId: {userId}, responseId: {_responseId}");

            StatusMessage = "Analizando tu cuestionario...";
            await Task.Delay(500); // Dar feedback visual

            // Llamar al servicio para generar la rutina
            var routine = await _aiRoutineService.GenerateRoutineAsync(userId, _responseId);

            if (routine == null)
            {
                System.Diagnostics.Debug.WriteLine("✗ No se pudo generar la rutina");

                await ErrorHandler.HandleErrorAsync(
                    "No se pudo generar tu rutina. " +
                    "Por favor, verifica tu conexión a internet e intenta nuevamente."
                );
                ResetToStart();
                return;
            }

            System.Diagnostics.Debug.WriteLine("✓ Rutina generada exitosamente");
            System.Diagnostics.Debug.WriteLine($"  Mensaje del coach: {routine.Message}");
            System.Diagnostics.Debug.WriteLine($"  Días de entrenamiento: {routine.Routine?.TrainingDays}");

            // Guardar la rutina generada
            GeneratedRoutine = routine;

            // Actualizar UI
            StatusMessage = "¡Rutina generada exitosamente!";
            IsGenerating = false;
            IsCompleted = true;

            System.Diagnostics.Debug.WriteLine("=== Generación completada ===");

            // Opcional: Navegar automáticamente a la vista de rutina
            // await Task.Delay(1000);
            // await NavigateToRoutineAsync();
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Debug.WriteLine($"✗ Error HTTP: {httpEx.Message}");

            await ErrorHandler.HandleErrorAsync(
                "Error de conexión con el servidor. " +
                "Verifica tu conexión a internet e intenta nuevamente."
            );
            ResetToStart();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"✗ Error inesperado: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

            await ErrorHandler.HandleErrorAsync(
                $"Ocurrió un error inesperado al generar tu rutina: {ex.Message}"
            );
            ResetToStart();
        }
    }

    /// <summary>
    /// Resetea el estado inicial para permitir reintentar
    /// </summary>
    private void ResetToStart()
    {
        IsGenerating = false;
        IsCompleted = false;
        ShowStartButton = true;
        StatusMessage = "Presiona el botón para generar tu rutina personalizada";
        GeneratedRoutine = null;

        System.Diagnostics.Debug.WriteLine("→ Estado reseteado a inicial");
    }

    #endregion
}
