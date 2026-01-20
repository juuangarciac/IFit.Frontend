using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.ExperienceLevel
{
    /// <summary>
    /// DTO para crear un nuevo nivel de experiencia
    /// </summary>
    public class CreateExperienceLevelDto
    {
        [JsonPropertyName("name")]
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("level")]
        [Required(ErrorMessage = "El nivel es obligatorio")]
        [Range(1, 10, ErrorMessage = "El nivel debe estar entre 1 y 10")]
        public int Level { get; set; }
    }
}