using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Questionnaire
{
    /// <summary>
    /// DTO de cuestionario con su primera pregunta incluida
    /// Útil para reducir llamadas al iniciar un cuestionario
    /// </summary>
    public class QuestionnaireWithFirstQuestionDto
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

        /// <summary>
        /// Primera pregunta del cuestionario con sus opciones
        /// </summary>
        [JsonPropertyName("firstQuestion")]
        public QuestionDTO FirstQuestion { get; set; } = new();
    }
}