using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Helper;
using IFit.Models;
using IFit.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace IFit.ViewModels;

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
    public partial bool IsGenerating { get; set; } = false;

    [ObservableProperty]
    public partial bool IsCompleted { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowStartButton { get; set; } = false;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructor con inyeccion de dependencias
    /// </summary>
    /// <param name="airoutineService"></param>
    public AIGenerationRoutineViewModel()
    {
        System.Diagnostics.Debug.WriteLine("=== Iniciando constructor AIGenerationRoutineViewModel ===");

        try
        {
            System.Diagnostics.Debug.WriteLine("Obteniendo AIRoutineService...");
            var aiService = App.GetService<AIRoutineService>()
                ?? throw new InvalidOperationException("AIRoutineService no registrado");
            System.Diagnostics.Debug.WriteLine("? AIRoutineService obtenido");

            System.Diagnostics.Debug.WriteLine("Obteniendo DatabaseService...");
            var dbService = App.GetService<DatabaseService>()
                ?? throw new InvalidOperationException("DatabaseService no registrado");
            System.Diagnostics.Debug.WriteLine("? DatabaseService obtenido");

            System.Diagnostics.Debug.WriteLine("Obteniendo responseId de Preferences...");
            var responseId = Preferences.Get("responseId", 0L);
            System.Diagnostics.Debug.WriteLine($"? ResponseId obtenido: {responseId}");

            _aiRoutineService = aiService;
            _databaseService = dbService;
            _responseId = responseId;

            ShowStartButton = true;

            System.Diagnostics.Debug.WriteLine("=== Constructor AIGenerationRoutineViewModel completado ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? Error en constructor: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    #endregion

    #region Commands

    [RelayCommand()]
    private async Task StartGenerationAsync()
    {
        if (IsGenerating) return;

        ShowStartButton = false;
        IsGenerating = true;
        IsCompleted = false;

        await GenerateRoutine();
    }

    #endregion

    #region Methods

    private async Task GenerateRoutine()
    {
        try
        {

            AppUser? appuser = await _databaseService.GetCurrentUserAsync();
            if (appuser == null)
            {
                await ErrorHandler.HandleErrorAsync(
                    "No hay ningún usuario activo. " +
                    "Por favor, inicia sesión para generar una rutina personalizada."
                );
                ResetToStart();
                return;
            }

            var prompt = await _aiRoutineService.GenerateRoutineFromQuestionnaire(appuser.Id, _responseId, appuser.CoachModelTypeName, true);
            if (prompt == null)
            {
                await ErrorHandler.HandleErrorAsync(
                    "No se pudo crear la solicitud para generar tu rutina. " +
                    "Verifica que tu perfil tenga toda la información necesaria (objetivos, nivel, etc.)."
                );
                ResetToStart();
                return;
            }

            // Revisar await AIRoutineService.GenerateRoutineFromQuestionnaire(prompt); 

            // Si llegamos aquí, la generación fue exitosa
            IsGenerating = false;
            IsCompleted = true;
        }
        catch (Exception ex)
        {
            await ErrorHandler.HandleErrorAsync(
                $"Ocurrió un error inesperado al generar tu rutina: {ex.Message}"
            );
            ResetToStart();
        }
    }

    private void ResetToStart()
    {
        IsGenerating = false;
        IsCompleted = false;
        ShowStartButton = true;
    }

    private async Task NavigateToRoutine()
    {
        // Navegar a la vista de la rutina
        await Shell.Current.GoToAsync("//HomeView"); // Ajusta la ruta según tu app
    }

    #endregion

}