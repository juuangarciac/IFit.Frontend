namespace IFit.Views.Components;

/// <summary>
/// Overlay de carga reutilizable que se puede usar en cualquier página.
/// Muestra un spinner con un mensaje personalizable.
/// </summary>
public partial class LoadingOverlay : ContentView
{
    #region Bindable Properties

    /// <summary>
    /// Propiedad bindable para controlar la visibilidad del overlay
    /// </summary>
    public static readonly BindableProperty IsLoadingProperty =
        BindableProperty.Create(
            nameof(IsLoading),
            typeof(bool),
            typeof(LoadingOverlay),
            defaultValue: false,
            propertyChanged: OnIsLoadingChanged);

    /// <summary>
    /// Propiedad bindable para el mensaje que se muestra
    /// </summary>
    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(
            nameof(Message),
            typeof(string),
            typeof(LoadingOverlay),
            defaultValue: "Cargando...",
            propertyChanged: OnMessageChanged);

    /// <summary>
    /// Propiedad interna para saber si hay mensaje (para mostrar/ocultar el Label)
    /// </summary>
    public static readonly BindableProperty HasMessageProperty =
        BindableProperty.Create(
            nameof(HasMessage),
            typeof(bool),
            typeof(LoadingOverlay),
            defaultValue: true);

    #endregion

    #region Public Properties

    /// <summary>
    /// Indica si el overlay está visible
    /// </summary>
    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    /// <summary>
    /// Mensaje que se muestra debajo del spinner
    /// </summary>
    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Indica si hay un mensaje para mostrar
    /// </summary>
    public bool HasMessage
    {
        get => (bool)GetValue(HasMessageProperty);
        private set => SetValue(HasMessageProperty, value);
    }

    #endregion

    #region Constructor

    public LoadingOverlay()
    {
        InitializeComponent();
    }

    #endregion

    #region Property Changed Handlers

    /// <summary>
    /// Se ejecuta cuando cambia IsLoading
    /// </summary>
    private static void OnIsLoadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        // Opcional: puedes agregar lógica aquí si necesitas hacer algo cuando cambia
        // Por ejemplo, logging:
        // var overlay = (LoadingOverlay)bindable;
        // Console.WriteLine($"LoadingOverlay IsLoading changed to: {newValue}");
    }

    /// <summary>
    /// Se ejecuta cuando cambia Message
    /// </summary>
    private static void OnMessageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var overlay = (LoadingOverlay)bindable;
        var message = newValue as string;

        // Actualizar HasMessage según si hay texto o no
        overlay.HasMessage = !string.IsNullOrWhiteSpace(message);
    }

    #endregion
}