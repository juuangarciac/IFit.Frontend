using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Questionnaire
{
    /// <summary>
    /// DTO para enviar una respuesta a una pregunta
    /// </summary>
    public class AnswerRequestDTO
    {
        /// <summary>
        /// ID de la pregunta que se está respondiendo
        /// </summary>
        [JsonPropertyName("questionId")]
        [Required(ErrorMessage = "El ID de la pregunta es obligatorio")]
        public long QuestionId { get; set; }

        /// <summary>
        /// ID de la opción seleccionada
        /// </summary>
        [JsonPropertyName("selectedOptionId")]
        [Required(ErrorMessage = "El ID de la opción seleccionada es obligatorio")]
        public long SelectedOptionId { get; set; }

        /// <summary>
        /// Texto adicional proporcionado por el usuario (solo si la opción lo requiere)
        /// </summary>
        [JsonPropertyName("additionalText")]
        public string? AdditionalText { get; set; }
    }
}