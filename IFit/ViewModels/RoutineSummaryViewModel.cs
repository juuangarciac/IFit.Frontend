using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Helper;
using IFit.Models.Dtos.AI;
using IFit.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.ViewModels
{
    public partial class RoutineSummaryViewModel : ObservableObject
    {
        #region Services
        // TODO: Descomentar cuando RoutineService esté implementado
        // private readonly RoutineService _routineService;
        #endregion

        #region Properties

        [ObservableProperty]
        private RoutineDto? _routine;

        [ObservableProperty]
        private string _messageAI = string.Empty;

        [ObservableProperty]
        private bool _isSaving = false;

        #endregion

        #region Constructor

        public RoutineSummaryViewModel()
        {
            // TODO: Descomentar cuando RoutineService esté implementado
            // _routineService = App.GetService<RoutineService>() 
            //     ?? throw new InvalidOperationException("RoutineService no registrado");

            // Si no se recibe la rutina por QueryProperty, intentar recuperarla de Preferences
            _ = LoadRoutineFromPreferencesIfNeeded();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Carga la rutina desde Preferences si no se recibió por navegación
        /// </summary>
        private async Task LoadRoutineFromPreferencesIfNeeded()
        {
            await Task.Delay(100); // Pequeño delay para que QueryProperty se aplique primero

            if (Routine == null)
            {
                var routineJson = Preferences.Get("TempRoutineData", string.Empty);
                if (!string.IsNullOrEmpty(routineJson))
                {
                    try
                    {
                        Routine = System.Text.Json.JsonSerializer.Deserialize<RoutineDto>(routineJson);
                        Preferences.Remove("TempRoutineData");
                    }
                    catch (Exception ex)
                    {
                        await ErrorHandler.HandleErrorAsync(
                            $"Error al cargar la rutina: {ex.Message}"
                        );
                    }
                }
            }

            if (string.IsNullOrEmpty(MessageAI))
            {
                MessageAI = Preferences.Get("TempMessageAI", string.Empty);
                if (!string.IsNullOrEmpty(MessageAI))
                {
                    Preferences.Remove("TempMessageAI");
                }
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Comando para volver a generar la rutina (navegar a AIGenerationRoutineView)
        /// </summary>
        [RelayCommand]
        private async Task TryAgainAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("//AIGenerationRoutineView");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(
                    $"Error al navegar a la generación de rutinas: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Comando para guardar la rutina que le ha gustado al usuario
        /// </summary>
        [RelayCommand]
        private async Task SaveRoutineAsync()
        {
            if (Routine == null)
            {
                await ErrorHandler.HandleErrorAsync("No hay ninguna rutina para guardar.");
                return;
            }

            try
            {
                IsSaving = true;

                // TODO: Descomentar cuando RoutineService esté implementado
                // await _routineService.SaveRoutineAsync(Routine);

                // Simulación temporal
                await Task.Delay(1000);

                await Shell.Current.DisplayAlert(
                    "¡Éxito!",
                    "Tu rutina ha sido guardada correctamente.",
                    "OK"
                );

                // Navegar a la página principal o a la vista de rutinas guardadas
                await Shell.Current.GoToAsync("///HomeView");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(
                    $"Error al guardar la rutina: {ex.Message}"
                );
            }
            finally
            {
                IsSaving = false;
            }
        }

        #endregion
    }
}
