using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Auth
{
    /// <summary>
    /// DTO de respuesta para logout
    /// </summary>
    public class LogoutResponseDto
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}