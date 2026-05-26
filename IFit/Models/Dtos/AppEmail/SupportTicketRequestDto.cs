namespace IFit.Models.Dtos.AppEmail
{
    public class SupportTicketRequestDto
    {
        public string Subject  { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message  { get; set; } = string.Empty;
    }
}
