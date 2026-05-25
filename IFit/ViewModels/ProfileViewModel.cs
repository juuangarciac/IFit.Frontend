using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos.Coach;
using IFit.Models.Dtos.ExperienceLevel;
using IFit.Services;

namespace IFit.ViewModels;

public class ProfileSelectionItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public partial class ProfileViewModel : ObservableObject
{
    #region Services

    private readonly AppUserService _appUserService;
    private readonly CoachModelTypeService _coachModelTypeService;
    private readonly ExperienceLevelService _experienceLevelService;

    #endregion

    #region Properties

    [ObservableProperty] public partial string UserName { get; set; } = string.Empty;
    [ObservableProperty] public partial string UserEmail { get; set; } = string.Empty;
    [ObservableProperty] public partial string UserInitials { get; set; } = "?";
    [ObservableProperty] public partial string AvatarSource { get; set; } = string.Empty;
    [ObservableProperty] public partial bool HasAvatar { get; set; } = false;
    [ObservableProperty] public partial string ExperienceLevel { get; set; } = "Sin asignar";
    [ObservableProperty] public partial string CoachModel { get; set; } = "Sin asignar";
    [ObservableProperty] public partial string MemberSince { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsLoading { get; set; } = true;
    [ObservableProperty] public partial bool IsSaving { get; set; } = false;
    [ObservableProperty] public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty] public partial bool IsCoachPanelExpanded { get; set; } = false;
    [ObservableProperty] public partial bool IsExperiencePanelExpanded { get; set; } = false;

    [ObservableProperty] public partial List<ProfileSelectionItem>? CoachOptions { get; set; }
    [ObservableProperty] public partial List<ProfileSelectionItem>? ExperienceOptions { get; set; }

    private List<CoachModelTypeResponseDto>? _coachModelTypes;
    private List<ExperienceLevelDto>? _experienceLevels;

    #endregion

    #region Constructor

    public ProfileViewModel(
        AppUserService appUserService,
        CoachModelTypeService coachModelTypeService,
        ExperienceLevelService experienceLevelService)
    {
        _appUserService = appUserService;
        _coachModelTypeService = coachModelTypeService;
        _experienceLevelService = experienceLevelService;
    }

    public ProfileViewModel() : this(
        App.GetService<AppUserService>() ?? throw new InvalidOperationException("AppUserService no está registrado"),
        App.GetService<CoachModelTypeService>() ?? throw new InvalidOperationException("CoachModelTypeService no está registrado"),
        App.GetService<ExperienceLevelService>() ?? throw new InvalidOperationException("ExperienceLevelService no está registrado"))
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

        var userTask = _appUserService.findUserById(userId);
        var coachesTask = _coachModelTypeService.GetCoachModelTypes();
        var levelsTask = _experienceLevelService.GetExperienceLevels();
        await Task.WhenAll(userTask, coachesTask, levelsTask);

        var user = userTask.Result;
        if (user == null)
        {
            StatusMessage = "No se ha podido cargar el perfil.";
            IsLoading = false;
            return;
        }

        _coachModelTypes = coachesTask.Result;
        _experienceLevels = levelsTask.Result;

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

        CoachOptions = _coachModelTypes?.Select(c => new ProfileSelectionItem
        {
            Id          = c.Id,
            Name        = c.Name,
            Description = c.Description,
            IsActive    = string.Equals(c.Name, CoachModel, StringComparison.OrdinalIgnoreCase)
        }).ToList();

        ExperienceOptions = _experienceLevels?.Select(l => new ProfileSelectionItem
        {
            Id          = l.Id,
            Name        = l.Name,
            Description = l.Description,
            IsActive    = string.Equals(l.Name, ExperienceLevel, StringComparison.OrdinalIgnoreCase)
        }).ToList();
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
        if (string.IsNullOrWhiteSpace(name)) return "?";
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
        if (_coachModelTypes != null && _experienceLevels != null) return;
        await InitializeAsync();
    }

    [RelayCommand]
    public void ToggleCoachPanel()
    {
        IsCoachPanelExpanded = !IsCoachPanelExpanded;
        if (IsCoachPanelExpanded) IsExperiencePanelExpanded = false;
    }

    [RelayCommand]
    public void ToggleExperiencePanel()
    {
        IsExperiencePanelExpanded = !IsExperiencePanelExpanded;
        if (IsExperiencePanelExpanded) IsCoachPanelExpanded = false;
    }

    [RelayCommand]
    public async Task SelectCoachAsync(ProfileSelectionItem item)
    {
        if (item.IsActive) { IsCoachPanelExpanded = false; return; }

        IsCoachPanelExpanded = false;
        IsLoading = true;
        StatusMessage = "Actualizando perfil...";

        var userId = Preferences.Get("UserId", 0L);
        var saveTask = _appUserService.SetCoachModelType(userId, item.Id);
        await Task.WhenAll(saveTask, Task.Delay(3000));

        var updatedUser = saveTask.Result ?? await _appUserService.findUserById(userId);
        if (updatedUser != null)
        {
            PopulateFromUser(updatedUser);
            Preferences.Set("CoachModelTypeName", CoachModel);
        }
        IsLoading = false;
    }

    [RelayCommand]
    public async Task SelectExperienceLevelAsync(ProfileSelectionItem item)
    {
        if (item.IsActive) { IsExperiencePanelExpanded = false; return; }

        IsExperiencePanelExpanded = false;
        IsLoading = true;
        StatusMessage = "Actualizando perfil...";

        var userId = Preferences.Get("UserId", 0L);
        var saveTask = _appUserService.SetExperienceLevel(userId, item.Id);
        await Task.WhenAll(saveTask, Task.Delay(3000));

        var updatedUser = saveTask.Result ?? await _appUserService.findUserById(userId);
        if (updatedUser != null) PopulateFromUser(updatedUser);
        IsLoading = false;
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
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
