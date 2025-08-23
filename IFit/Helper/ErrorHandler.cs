using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.Helper
{
   public static class ErrorHandler
    {
        public static async Task HandleErrorAsync(
            string debugMessage,
            string? navigationRoute = null,
            string? alertTitle = null,
            string? alertMessage = null)
        {
            Debug.WriteLine(debugMessage);

            if (!string.IsNullOrEmpty(alertTitle) &&
                !string.IsNullOrEmpty(alertMessage) &&
                App.Current?.MainPage != null)
            {
                await App.Current.MainPage.DisplayAlert(alertTitle, alertMessage, "OK");
            }

            if (!string.IsNullOrEmpty(navigationRoute))
            {
                await Shell.Current.GoToAsync(navigationRoute);
            }
        }
    }

}

