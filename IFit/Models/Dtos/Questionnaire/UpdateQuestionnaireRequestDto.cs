using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Questionnaire
{
    /// <summary>
    /// DTO para actualizar un cuestionario existente (solo administradores)
    /// </summary>
    public class UpdateQuestionnaireRequestDto
    {
        [JsonPropertyName("name")]
        [StringLength(200, MinimumLength = 3)]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [JsonPropertyName("coachModelTypeId")]
        public long? CoachModelTypeId { get; set; }

        [JsonPropertyName("experienceLevelId")]
        public long? ExperienceLevelId { get; set; }

        [JsonPropertyName("isEnabled")]
        public bool? IsEnabled { get; set; }
    }
}