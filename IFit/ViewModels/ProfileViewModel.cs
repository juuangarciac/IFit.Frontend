using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Services;
using System.Diagnostics;

namespace IFit.ViewModels;

[QueryProperty(nameof(User), "User")]
public partial class ProfileViewModel : ObservableObject
{
    #region Properties

    [ObservableProperty]
    public partial AppUserResponseDto? User { get; set; }

    // Called automatically by MVVM Toolkit when User is set via QueryProperty
    partial void OnUserChanged(AppUserResponseDto? value)
    {
        if (value == null) return;
        PopulateFromUser(value);
        _isInitialized = true;
    }

    [ObservableProperty]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UserEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UserInitials { get; set; } = "?";

    [ObservableProperty]
    public partial string AvatarSource { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasAvatar { get; set; } = false;

    [ObservableProperty]
    public partial string ExperienceLevel { get; set; } = "Sin asignar";

    [ObservableProperty]
    public partial string CoachModel { get; set; } = "Sin asignar";

    [ObservableProperty]
    public partial string MemberSince { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    #endregion

    #region Services

    private readonly AppUserService _appUserService;

    #endregion

    private bool _isInitialized = false;

    #region Constructor

    public ProfileViewModel(AppUserService appUserService)
    {
        _appUserService = appUserService;
    }

    public ProfileViewModel() : this(
        App.GetService<AppUserService>() ?? throw new InvalidOperationException("AppUserService no está registrado"))
    {
    }

    #endregion

    #region Methods

    private async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "Cargando perfil...";

        var userId = Preferences.Get("UserId", 0L);

        if (userId <= 0)
        {
            StatusMessage = "No se ha encontrado el usuario actual.";
            IsLoading = false;
            return;
        }

        var user = await _appUserService.findUserById(userId);

        if (user == null)
        {
            StatusMessage = "No se ha podido cargar el perfil.";
            IsLoading = false;
            return;
        }

        User = user;
        PopulateFromUser(user);
        IsLoading = false;
    }

    private void PopulateFromUser(AppUserResponseDto user)
    {
        UserName        = user.Name;
        UserEmail       = user.Email;
        UserInitials    = GetInitials(user.Name);
        ExperienceLevel = user.ExperienceLevelName ?? "Sin asignar";
        CoachModel      = user.CoachModelTypeName ?? "Sin asignar";
        MemberSince     = user.CreatedAt.ToString("MMMM yyyy");
        AvatarSource    = GetAvatarSource(user.CoachModelTypeName);
        HasAvatar       = !string.IsNullOrEmpty(AvatarSource);
        IsLoading       = false;
    }

    private static string GetAvatarSource(string? coachModelName) =>
        coachModelName?.ToLowerInvariant() switch
        {
            "ronnie" or "kael" or "eliud" => "icons8coachmale.png",
            "serena"                       => "icons8coachfemale.png",
            _                              => string.Empty
        };

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
            : $"{parts[0][0]}".ToUpper();
    }

    #endregion

    #region Commands

    [RelayCommand]
    public async Task AppearingAsync()
    {
        if (_isInitialized) return;
        await InitializeAsync();
        _isInitialized = true;
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Cerrar sesión",
            "¿Estás seguro de que quieres cerrar sesión?",
            "Sí",
            "Cancelar");

        if (!confirm)
            return;

        Preferences.Clear();
        await Shell.Current.GoToAsync("//SignInView");
    }

    [RelayCommand]
    public async Task GoToHomeAsync()
    {
        await Shell.Current.GoToAsync("//HomeView");
    }

    #endregion
}
