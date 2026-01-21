using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Questionnaire
{
    /// <summary>
    /// DTO que representa una pregunta del cuestionario
    /// </summary>
    public class QuestionDTO
    {
        /// <summary>
        /// ID único de la pregunta
        /// </summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>
        /// Texto de la pregunta
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de pregunta (MULTIPLE_CHOICE, TEXT_INPUT, etc.)
        /// </summary>
        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public QuestionType Type { get; set; }

        /// <summary>
        /// Lista de opciones de respuesta para esta pregunta
        /// </summary>
        [JsonPropertyName("options")]
        public List<OptionDTO> Options { get; set; } = new();
    }
}