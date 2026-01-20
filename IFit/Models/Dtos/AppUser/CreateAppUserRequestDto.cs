using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.AppUser
{

    namespace IFit.Models.Dtos.User
    {
        /// <summary>
        /// DTO para crear un nuevo usuario
        /// </summary>
        public class CreateAppUserRequestDto
        {
            [JsonPropertyName("name")]
            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("email")]
            [Required(ErrorMessage = "El email es obligatorio")]
            [EmailAddress(ErrorMessage = "Email inválido")]
            public string Email { get; set; } = string.Empty;

            [JsonPropertyName("password")]
            [Required(ErrorMessage = "La contraseña es obligatoria")]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
            public string Password { get; set; } = string.Empty;
        }
    }
}
