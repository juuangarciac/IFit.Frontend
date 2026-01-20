using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Auth
{
    /// <summary>
    /// DTO para solicitudes de refresh y logout
    /// </summary>
    public class RefreshTokenRequestDto
    {
        [JsonPropertyName("refreshToken")]
        [Required(ErrorMessage = "El refresh token es obligatorio")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}