using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Questionnaire
{
    /// <summary>
    /// DTO resumido de cuestionario para listados
    /// </summary>
    public class QuestionnaireSummaryDto
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
    }
}