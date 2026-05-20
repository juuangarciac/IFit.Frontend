using IFit.Helper;
using IFit.Views.Components;
using System.Diagnostics;

namespace IFit.Services
{
    /// <summary>
    /// Servicio estático para mostrar notificaciones toast personalizadas sobre la página actual.
    /// Sustituye a DisplayAlert para mensajes de éxito, error e información.
    ///
    /// El toast se inyecta directamente en el Grid raíz de la ContentPage visible y se anima
    /// con slide-up + fade in/out. Si ya hay uno activo, lo reemplaza sin animación.
    /// </summary>
    public static class NotificationService
    {
        private static ToastNotification? _activeToast;
        private static Grid? _activeGrid;

        // ─── API pública ──────────────────────────────────────────────────────────

        public static Task ShowSuccessAsync(string message, int durationMs = 2500)
            => ShowAsync(message, NotificationType.Success, durationMs);

        public static Task ShowErrorAsync(string message, int durationMs = 3500)
            => ShowAsync(message, NotificationType.Error, durationMs);

        public static Task ShowInfoAsync(string message, int durationMs = 2500)
            => ShowAsync(message, NotificationType.Info, durationMs);

        // ─── Implementación ───────────────────────────────────────────────────────

        private static Task ShowAsync(string message, NotificationType type, int durationMs)
        {
            // Garantizamos ejecución en el hilo de UI y devolvemos la Task completa
            // usando el overload Func<Task> para que el caller pueda awaitar si necesita
            return MainThread.InvokeOnMainThreadAsync(() => ShowOnMainThreadAsync(message, type, durationMs));
        }

        private static async Task ShowOnMainThreadAsync(string message, NotificationType type, int durationMs)
        {
            try
            {
                // ── 1. Retirar toast anterior si lo hay ──────────────────────────
                DismissActiveToast();

                // ── 2. Obtener la ContentPage activa ────────────────────────────
                // Shell.Current.CurrentPage devuelve Page (clase base, sin Content).
                // Necesitamos ContentPage para acceder a su propiedad Content.
                var contentPage = Shell.Current?.CurrentPage as ContentPage;
                if (contentPage == null)
                {
                    Debug.WriteLine("NotificationService: CurrentPage no es ContentPage, omitiendo toast.");
                    return;
                }

                // ── 3. Obtener el Grid raíz ──────────────────────────────────────
                if (contentPage.Content is not Grid rootGrid)
                {
                    Debug.WriteLine($"NotificationService: el root de '{contentPage.GetType().Name}' " +
                                    $"es {contentPage.Content?.GetType().Name ?? "null"}, necesita ser Grid.");
                    return;
                }

                // ── 4. Crear y configurar el toast ───────────────────────────────
                var toast = new ToastNotification();
                toast.Initialize(message, type);
                toast.VerticalOptions = LayoutOptions.Start;
                toast.ZIndex = 9999;
                toast.Opacity = 0;
                toast.TranslationY = -90;

                // Span sobre todas las filas para posicionarse al fondo del Grid
                int rowSpan = Math.Max(rootGrid.RowDefinitions.Count, 1);
                Grid.SetRow(toast, 0);
                Grid.SetRowSpan(toast, rowSpan);

                _activeToast = toast;
                _activeGrid = rootGrid;
                rootGrid.Add(toast);

                // ── 5. Animación de entrada: slide-up + fade in ──────────────────
                await Task.WhenAll(
                    toast.FadeTo(1, 280, Easing.CubicOut),
                    toast.TranslateTo(0, 0, 280, Easing.CubicOut)
                );

                await Task.Delay(durationMs);

                // ── 6. Salir solo si este toast sigue siendo el activo ───────────
                if (_activeToast != toast)
                    return;

                // ── 7. Animación de salida: slide-down + fade out ────────────────
                await Task.WhenAll(
                    toast.FadeTo(0, 250, Easing.CubicIn),
                    toast.TranslateTo(0, -90, 250, Easing.CubicIn)
                );

                // Comprobación final por si llegó otro toast durante la salida
                if (_activeToast == toast)
                {
                    rootGrid.Remove(toast);
                    _activeToast = null;
                    _activeGrid = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NotificationService error: {ex.Message}");
                DismissActiveToast();
            }
        }

        /// <summary>
        /// Retira el toast activo del Grid sin animación (para reemplazos inmediatos).
        /// Debe llamarse desde el hilo de UI.
        /// </summary>
        private static void DismissActiveToast()
        {
            if (_activeToast == null || _activeGrid == null)
                return;

            try
            {
                _activeGrid.Remove(_activeToast);
            }
            catch
            {
                // El grid ya podría haber sido destruido por navegación
            }
            finally
            {
                _activeToast = null;
                _activeGrid = null;
            }
        }
    }
}
