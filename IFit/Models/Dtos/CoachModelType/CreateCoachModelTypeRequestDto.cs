using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Coach
{
    /// <summary>
    /// DTO para crear un nuevo tipo de modelo de coach
    /// </summary>
    public class CreateCoachModelTypeRequestDto
    {
        [JsonPropertyName("name")]
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("emoji")]
        [StringLength(10, ErrorMessage = "El emoji no puede exceder 10 caracteres")]
        public string? Emoji { get; set; }

        [JsonPropertyName("type")]
        [Required(ErrorMessage = "El tipo es obligatorio")]
        [StringLength(50, ErrorMessage = "El tipo no puede exceder 50 caracteres")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
    }
}