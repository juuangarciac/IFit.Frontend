using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos.AppEmail;
using IFit.Services;
using System.Diagnostics;

namespace IFit.ViewModels;

public partial class HelpViewModel : ObservableObject
{
    #region Properties

    [ObservableProperty]
    public partial string Subject { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedCategory { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    #endregion

    #region Services

    private readonly AppEmailService _appEmailService;

    #endregion

    #region Constructor

    public HelpViewModel(AppEmailService appEmailService)
    {
        _appEmailService = appEmailService
            ?? throw new ArgumentNullException(nameof(appEmailService));
    }

    public HelpViewModel() : this(
        App.GetService<AppEmailService>()
            ?? throw new InvalidOperationException("AppEmailService no registrado"))
    { }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("//HomeView");
    }

    [RelayCommand]
    private void SelectCategory(string category)
    {
        SelectedCategory = SelectedCategory == category ? string.Empty : category;
    }

    [RelayCommand]
    private async Task SendTicketAsync()
    {
        if (string.IsNullOrWhiteSpace(Subject))
        {
            await NotificationService.ShowErrorAsync("Por favor, introduce un asunto.");
            return;
        }
        if (string.IsNullOrWhiteSpace(SelectedCategory))
        {
            await NotificationService.ShowErrorAsync("Por favor, selecciona una categoría.");
            return;
        }
        if (string.IsNullOrWhiteSpace(Message))
        {
            await NotificationService.ShowErrorAsync("Por favor, describe tu problema.");
            return;
        }

        try
        {
            IsLoading = true;

            var request = new SupportTicketRequestDto
            {
                Subject  = Subject.Trim(),
                Category = SelectedCategory,
                Message  = Message.Trim()
            };

            var response = await _appEmailService.SendSupportTicketAsync(request);

            if (response == null || !response.IsSend)
            {
                var errorMsg = response?.Message ?? "No se pudo enviar el ticket. Inténtalo de nuevo.";
                await NotificationService.ShowErrorAsync(errorMsg);
                return;
            }

            Subject = string.Empty;
            SelectedCategory = string.Empty;
            Message = string.Empty;

            await NotificationService.ShowSuccessAsync("Ticket enviado. Te responderemos por email en menos de 24h.");
            await Shell.Current.GoToAsync("//HomeView");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ Error enviando ticket: {ex.Message}");
            await NotificationService.ShowErrorAsync("Error inesperado al enviar el ticket.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}
