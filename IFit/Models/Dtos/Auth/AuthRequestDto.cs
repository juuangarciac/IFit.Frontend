using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Auth
{
    /// <summary>
    /// DTO para solicitud de login
    /// </summary>
    public class AuthRequestDto
    {
        [JsonPropertyName("username")]
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Password { get; set; } = string.Empty;
    }
}