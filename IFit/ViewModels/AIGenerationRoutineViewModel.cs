using IFit.Helper;
using IFit.Models;
using IFit.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace IFit.ViewModels;

public class AIGenerationRoutineViewModel : INotifyPropertyChanged
{
    private bool _isGenerating = false;
    private bool _isCompleted = false;
    private bool _showStartButton = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsGenerating
    {
        get => _isGenerating;
        set
        {
            _isGenerating = value;
            OnPropertyChanged();
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            _isCompleted = value;
            OnPropertyChanged();
        }
    }

    public bool ShowStartButton
    {
        get => _showStartButton;
        set
        {
            _showStartButton = value;
            OnPropertyChanged();
        }
    }

    public ICommand StartGenerationCommand { get { return new Command(async () => await StartGeneration());  } }
    public ICommand ViewRoutineCommand { get { return new Command(async () => await NavigateToRoutine()); } }

    public AIGenerationRoutineViewModel() { }

    private async Task StartGeneration()
    {
        if (IsGenerating) return;

        ShowStartButton = false;
        IsGenerating = true;
        IsCompleted = false;

        await GenerateRoutine();
    }

    private async Task GenerateRoutine()
    {
        try
        {
            AIRoutineService? AIRoutineService = App.GetService<AIRoutineService>();
            DatabaseService? databaseService = App.GetService<DatabaseService>();

            if (AIRoutineService == null || databaseService == null)
            {
                await ErrorHandler.HandleErrorAsync(
                    "No se pudieron inicializar los servicios necesarios. " +
                    "Por favor, reinicia la aplicación."
                );
                ResetToStart();
                return;
            }

            AppUser? appuser = await databaseService.GetCurrentUserAsync();
            if (appuser == null)
            {
                await ErrorHandler.HandleErrorAsync(
                    "No hay ningún usuario activo. " +
                    "Por favor, inicia sesión para generar una rutina personalizada."
                );
                ResetToStart();
                return;
            }

            var prompt = await AIRoutineService.CreateRoutinePromptForAI(appuser);
            if (prompt == null)
            {
                await ErrorHandler.HandleErrorAsync(
                    "No se pudo crear la solicitud para generar tu rutina. " +
                    "Verifica que tu perfil tenga toda la información necesaria (objetivos, nivel, etc.)."
                );
                ResetToStart();
                return;
            }

            await AIRoutineService.GenerateRoutineFromAI(appuser, prompt);

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

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}