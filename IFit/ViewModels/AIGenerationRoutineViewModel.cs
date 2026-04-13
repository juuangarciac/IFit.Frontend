using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Helper;
using IFit.Models;
using IFit.Models.Dtos.AI;
using IFit.Models.Dtos.Questionnaire;
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
    private readonly QuestionnaireService _questionnaireService;
    private readonly DatabaseService _databaseService;
    #endregion

    #region Fields
    private readonly long _responseId;
    #endregion

    #region Properties

    [ObservableProperty]
    private QuestionnaireResponseSummaryDTO _questionnaireSummary;

    [ObservableProperty]
    private string _questionnaireName = string.Empty;

    [ObservableProperty]
    private string _coachName = string.Empty;

    [ObservableProperty]
    private bool _isGenerating = false;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _statusMessage = "Cargando...";

    [ObservableProperty]
    private RoutineResponseDto? _generatedRoutine;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructor con inyección de dependencias
    /// </summary>
    /// 

    public AIGenerationRoutineViewModel(AIRoutineService aiService,
        DatabaseService dbService,
        QuestionnaireService questionnaireService,
        long responseId)
    { 
        _aiRoutineService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _databaseService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        _questionnaireService = questionnaireService ?? throw new ArgumentNullException(nameof(questionnaireService));
        _responseId = responseId;
    }

    public AIGenerationRoutineViewModel() :this(
        App.GetService<AIRoutineService>() ?? throw new InvalidOperationException("AIRoutineService no registrado"),
        App.GetService<DatabaseService>() ?? throw new InvalidOperationException("DatabaseService no registrado"),
        App.GetService<QuestionnaireService>() ?? throw new InvalidOperationException("QuestionnaireService no registrado"),
        Preferences.Get("responseId", 0L)
    )
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()

    {
        // Obtener resumen del cuestionario para mostrar en la UI
        try
        {
            StatusMessage = "Obteniendo resumen de tu cuestionario...";
            IsLoading = true;

            QuestionnaireResponseSummaryDTO? summary = await _questionnaireService.GetResponseSummary(_responseId);
            if (summary == null)
            {
                await ErrorHandler.HandleErrorAsync(
                    "No se pudo obtener el resumen de tu cuestionario. " +
                    "Por favor, verifica tu conexión a internet e intenta nuevamente."
                );
                ResetToStart();
                return;
            }

            QuestionnaireSummary = summary;
            QuestionnaireName = summary.QuestionnaireName;
            CoachName = Preferences.Get("CoachName", "");

            await GenerateRoutineAsync();
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(
                $"Ocurrió un error al obtener el resumen de tu cuestionario: {ex.Message}"
            );
            ResetToStart();
        }
        finally 
        {
           IsLoading = false;
            StatusMessage = string.Empty;
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
            return;
        }

        try
        {
            var navigationParameter = new Dictionary<String, Object>() 
            {
                {"Routine", GeneratedRoutine }
            };

            await Shell.Current.GoToAsync("//RoutineSummaryView", navigationParameter);
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync($"Error al mostrar la rutina: {ex.Message}");
        }
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
            // Obtener usuario actual
            AppUser? appUser = await _databaseService.GetCurrentUserAsync();
            if (appUser == null)
            {
                await ErrorHandler.HandleErrorAsync(
                    "No hay ningún usuario activo. " +
                    "Por favor, inicia sesión para generar una rutina personalizada."
                );
                ResetToStart();
                return;
            }

            // Validar responseId
            if (_responseId <= 0)
            {
                await ErrorHandler.HandleErrorAsync(
                    "No se encontró un cuestionario completado. " +
                    "Por favor, completa el cuestionario antes de generar tu rutina."
                );
                ResetToStart();
                return;
            }

            // Convertir el ID de usuario a String (como espera el backend)
            string userId = appUser.Id.ToString();

            StatusMessage = "Analizando tu cuestionario...";
            await Task.Delay(500); // Dar feedback visual

            // Llamar al servicio para generar la rutina
            string? coachType = Preferences.Get("CoachName", "");
            if (string.IsNullOrWhiteSpace(coachType)) coachType = null;
            var routine = await _aiRoutineService.GenerateRoutineAsync(userId, _responseId, coachType);
            // var routine = _aiRoutineService.GenerateTestRoutine();

            if (routine == null)
            {
                await ErrorHandler.HandleErrorAsync(
                    "No se pudo generar tu rutina. " +
                    "Por favor, verifica tu conexión a internet e intenta nuevamente."
                );
                ResetToStart();
                return;
            }

            // Guardar la rutina generada
            GeneratedRoutine = routine;

            // Actualizar UI
            StatusMessage = "¡Rutina generada exitosamente!";
            await NavigateToRoutineAsync();
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
        StatusMessage = "Presiona el botón para generar tu rutina personalizada";
        GeneratedRoutine = null;
    }

    #endregion
}
