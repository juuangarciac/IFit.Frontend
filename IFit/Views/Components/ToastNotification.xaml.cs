using IFit.Helper;

namespace IFit.Views.Components;

public partial class ToastNotification : ContentView
{
    public ToastNotification()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Configura el contenido visual del toast según el tipo de notificación.
    /// </summary>
    public void Initialize(string message, NotificationType type)
    {
        messageLabel.Text = message;

        var (accentColor, icon) = type switch
        {
            NotificationType.Success => (Color.FromArgb("#FFD369"), "✓"),
            NotificationType.Error   => (Color.FromArgb("#E05252"), "✗"),
            NotificationType.Info    => (Color.FromArgb("#8FA0B0"), "ℹ"),
            _                        => (Color.FromArgb("#8FA0B0"), "ℹ")
        };

        accentBar.Color = accentColor;
        iconLabel.Text = icon;
        iconLabel.TextColor = accentColor;
    }
}
