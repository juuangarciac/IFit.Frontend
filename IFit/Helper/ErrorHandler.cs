using IFit.Services;
using System.Diagnostics;

namespace IFit.Helper
{
    public static class ErrorHandler
    {
        /// <summary>
        /// Muestra un toast de error (si se facilita alertMessage) y navega opcionalmente.
        /// Los parámetros alertTitle y alertMessage se mantienen por compatibilidad;
        /// alertTitle ya no se usa porque los toasts no tienen título.
        /// </summary>
        public static async Task HandleErrorAsync(
            string debugMessage,
            string? navigationRoute = null,
            string? alertTitle = null,
            string? alertMessage = null)
        {
            Debug.WriteLine(debugMessage);

            if (!string.IsNullOrEmpty(alertMessage))
            {
                await NotificationService.ShowErrorAsync(alertMessage);
            }

            if (!string.IsNullOrEmpty(navigationRoute))
            {
                await Shell.Current.GoToAsync(navigationRoute);
            }
        }
    }
}


