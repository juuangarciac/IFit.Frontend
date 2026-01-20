using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.ExperienceLevel
{
    /// <summary>
    /// DTO para actualizar un nivel de experiencia existente
    /// Todos los campos son opcionales para soportar actualizaciones parciales
    /// </summary>
    public class UpdateExperienceLevelDto
    {
        [JsonPropertyName("name")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Description { get; set; }

        [JsonPropertyName("level")]
        [Range(1, 10, ErrorMessage = "El nivel debe estar entre 1 y 10")]
        public int? Level { get; set; }
    }
}