using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Questionnaire
{
    /// <summary>
    /// DTO que representa la respuesta después de iniciar un cuestionario o responder una pregunta.
    /// Contiene el estado actual de la sesión y la siguiente pregunta a mostrar.
    /// </summary>
    public class QuestionnaireResponseDTO
    {
        /// <summary>
        /// ID de la sesión de respuesta del cuestionario
        /// </summary>
        [JsonPropertyName("responseId")]
        public long ResponseId { get; set; }

        /// <summary>
        /// Pregunta actual a mostrar al usuario (null si ya completó)
        /// </summary>
        [JsonPropertyName("currentQuestion")]
        public QuestionDTO? CurrentQuestion { get; set; }

        /// <summary>
        /// Indica si el cuestionario ha sido completado
        /// </summary>
        [JsonPropertyName("isCompleted")]
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Número total de preguntas respondidas hasta ahora
        /// </summary>
        [JsonPropertyName("totalQuestionsAnswered")]
        public int TotalQuestionsAnswered { get; set; }
    }
}