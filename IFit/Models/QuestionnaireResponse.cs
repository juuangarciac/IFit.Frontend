using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Questionnaire
{
    /// <summary>
    /// Modelo de sesión de respuesta de cuestionario
    /// </summary>
    public class QuestionnaireResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("userId")]
        public long UserId { get; set; }

        [JsonPropertyName("questionnaireId")]
        public long QuestionnaireId { get; set; }

        [JsonPropertyName("questionnaireName")]
        public string QuestionnaireName { get; set; } = string.Empty;

        [JsonPropertyName("startedAt")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("isCompleted")]
        public bool IsCompleted { get; set; }

        [JsonPropertyName("totalQuestionsAnswered")]
        public int TotalQuestionsAnswered { get; set; }
    }
}