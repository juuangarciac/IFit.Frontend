using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Questionnaire
{
    /// <summary>
    /// DTO para crear un nuevo cuestionario (solo administradores)
    /// </summary>
    public class CreateQuestionnaireRequestDto
    {
        [JsonPropertyName("name")]
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("coachModelTypeId")]
        [Required(ErrorMessage = "El tipo de coach es obligatorio")]
        public long CoachModelTypeId { get; set; }

        [JsonPropertyName("experienceLevelId")]
        [Required(ErrorMessage = "El nivel de experiencia es obligatorio")]
        public long ExperienceLevelId { get; set; }

        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; } = true;
    }
}