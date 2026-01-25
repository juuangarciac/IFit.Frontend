using IFit.Models.Dtos.AppUser.IFit.Models.Dtos.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFit.Models.Dtos.Auth
{
    /// <summary>
    /// Modelo que representa la respuesta completa de login/register
    /// </summary>
    public class AuthResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("tokenType")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("appUser")]
        public AppUserResponseDto? AppUser { get; set; }

        /// <summary>
        /// Gestionar mensaje de error recibido desde la API.
        /// </summary>
        [JsonIgnore]
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
