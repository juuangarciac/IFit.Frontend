using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.AppEmail
{
    public class SupportTicketRequestDto
    {
        [JsonPropertyName("subject")]
        public string Subject  { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message  { get; set; } = string.Empty;
    }
}
