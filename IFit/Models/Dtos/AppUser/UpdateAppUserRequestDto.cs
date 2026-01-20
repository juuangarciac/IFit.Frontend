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
        /// DTO para actualizar datos de un usuario existente
        /// Todos los campos son opcionales para soportar actualizaciones parciales
        /// </summary>
        public class UpdateAppUserRequestDto
        {
            [JsonPropertyName("name")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
            public string? Name { get; set; }

            [JsonPropertyName("email")]
            [EmailAddress(ErrorMessage = "Email inválido")]
            public string? Email { get; set; }

            [JsonPropertyName("password")]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
            public string? Password { get; set; }

            [JsonPropertyName("coachModelTypeId")]
            public long? CoachModelTypeId { get; set; }

            [JsonPropertyName("experienceLevelId")]
            public long? ExperienceLevelId { get; set; }

            [JsonPropertyName("registrationCompleted")]
            public bool? RegistrationCompleted { get; set; }
        }
    }
}
