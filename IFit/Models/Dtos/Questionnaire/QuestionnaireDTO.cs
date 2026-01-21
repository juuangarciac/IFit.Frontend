using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Questionnaire
{
    /// <summary>
    /// DTO completo de cuestionario con todos sus detalles
    /// </summary>
    public class QuestionnaireDTO
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("coachModelTypeName")]
        public string? CoachModelTypeName { get; set; }

        [JsonPropertyName("coachModelTypeEmoji")]
        public string? CoachModelTypeEmoji { get; set; }

        [JsonPropertyName("experienceLevelName")]
        public string? ExperienceLevelName { get; set; }

        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("questions")]
        public List<QuestionDTO> Questions { get; set; } = new();
    }
}