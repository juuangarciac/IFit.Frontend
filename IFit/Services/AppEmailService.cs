using IFit.Models.Dtos.AppEmail;
using System.Diagnostics;

namespace IFit.Services
{
    public class AppEmailService
    {
        private readonly WebService _webService;

        public AppEmailService(WebService webService)
        {
            _webService = webService ?? throw new ArgumentNullException(nameof(webService));
        }

        public async Task<SupportTicketResponseDto?> SendSupportTicketAsync(SupportTicketRequestDto request)
        {
            try
            {
                var response = await _webService.PostAsync<SupportTicketRequestDto, SupportTicketResponseDto>(
                    "/appemail/support-ticket", request, requiresAuth: true);

                if (!response.Success)
                {
                    Debug.WriteLine($"✗ Error enviando ticket de soporte: {response.ErrorMessage}");
                    return null;
                }

                Debug.WriteLine($"✓ Ticket enviado — isSend={response.Data?.IsSend}");
                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Excepción en SendSupportTicketAsync: {ex.Message}");
                return null;
            }
        }
    }
}
