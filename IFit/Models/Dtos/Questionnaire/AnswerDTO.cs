using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Questionnaire
{
    /// <summary>
    /// DTO que representa una respuesta del usuario a una pregunta
    /// </summary>
    public class AnswerDTO
    {
        [JsonPropertyName("answerId")]
        public long AnswerId { get; set; }

        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("selectedOption")]
        public string SelectedOptionText { get; set; } = string.Empty;

        [JsonPropertyName("additionalText")]
        public string? AdditionalText { get; set; }

        [JsonPropertyName("answeredAt")]
        public DateTime AnsweredAt { get; set; }
    }
}