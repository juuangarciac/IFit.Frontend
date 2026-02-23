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
    [QueryProperty(nameof(Routine), "Routine")]
    public partial class RoutineSummaryViewModel : ObservableObject
    {
        #region Services
        // TODO: Descomentar cuando RoutineService esté implementado
        // private readonly RoutineService _routineService;
        #endregion

        #region Properties

        [ObservableProperty]
        private RoutineResponseDto? _routine;

        [ObservableProperty]
        private string _messageAI = string.Empty;

        [ObservableProperty]
        private bool _isSaving = false;

        #endregion

        #region Constructor

        public RoutineSummaryViewModel()
        {

        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Carga la rutina desde Preferences si no se recibió por navegación
        /// </summary>
        partial void OnRoutineChanged(RoutineResponseDto? value)
        {
            if (value != null)
            {
                MessageAI = value.Description;
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
