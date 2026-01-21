using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Questionnaire
{
    /// <summary>
    /// DTO de resumen completo de una sesión de cuestionario
    /// </summary>
    public class QuestionnaireResponseSummaryDTO
    {
        [JsonPropertyName("responseId")]
        public long ResponseId { get; set; }

        [JsonPropertyName("questionnaireName")]
        public string QuestionnaireName { get; set; } = string.Empty;

        [JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("startedAt")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("isCompleted")]
        public bool IsCompleted { get; set; }

        [JsonPropertyName("answers")]
        public List<AnswerDTO> Answers { get; set; } = new();
    }
}