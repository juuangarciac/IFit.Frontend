using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Questionnaire
{
    /// <summary>
    /// DTO que representa una respuesta del usuario a una pregunta
    /// </summary>
    public class AnswerDTO
    {
        [JsonPropertyName("questionId")]
        public long QuestionId { get; set; }

        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("selectedOptionId")]
        public long SelectedOptionId { get; set; }

        [JsonPropertyName("selectedOptionText")]
        public string SelectedOptionText { get; set; } = string.Empty;

        [JsonPropertyName("additionalText")]
        public string? AdditionalText { get; set; }

        [JsonPropertyName("answeredAt")]
        public DateTime AnsweredAt { get; set; }
    }
}