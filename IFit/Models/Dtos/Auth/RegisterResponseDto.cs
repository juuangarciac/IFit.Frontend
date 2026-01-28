using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFit.Models.Dtos.Auth
{
    /// <summary>
    /// DTO para respuesta de registro de nuevo usuario
    /// </summary>
    public class RegisterResponseDto
    {
        [JsonPropertyName("success")]
        [Required(ErrorMessage = "El campo success es obligatorio")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        [Required(ErrorMessage = "El campo message es obligatorio")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        [Required(ErrorMessage = "El campo email es obligatorio")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("requiresEmailVerification")]
        [Required(ErrorMessage = "El campo requiresEmailVerification es obligatorio")]
        public bool RequiresEmailVerification { get; set; }
    }
}
