using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos.AppEmail;
using IFit.Services;
using System.Diagnostics;

namespace IFit.ViewModels;

public partial class SupportTicketViewModel : ObservableObject
{
    #region Properties

    [ObservableProperty]
    public partial string Subject { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedCategory { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSending { get; set; }

    public List<string> Categories { get; } = ["TECNICO", "RUTINA", "CUENTA", "OTRO"];

    #endregion

    #region Services

    private readonly AppEmailService _appEmailService;

    #endregion

    #region Constructor

    public SupportTicketViewModel(AppEmailService appEmailService)
    {
        _appEmailService = appEmailService
            ?? throw new ArgumentNullException(nameof(appEmailService));
    }

    public SupportTicketViewModel() : this(
        App.GetService<AppEmailService>()
            ?? throw new InvalidOperationException("AppEmailService no registrado"))
    { }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
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
            IsSending = true;

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

            await NotificationService.ShowSuccessAsync("Ticket enviado. Te responderemos por email en menos de 24h.");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ Error enviando ticket: {ex.Message}");
            await NotificationService.ShowErrorAsync("Error inesperado al enviar el ticket.");
        }
        finally
        {
            IsSending = false;
        }
    }

    #endregion
}
