using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Questionnaire
{
    /// <summary>
    /// DTO que representa una opción de respuesta para una pregunta
    /// </summary>
    public class OptionDTO
    {
        /// <summary>
        /// ID único de la opción
        /// </summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>
        /// Texto de la opción
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Indica si esta opción requiere que el usuario ingrese texto adicional
        /// </summary>
        [JsonPropertyName("requiresTextInput")]
        public bool RequiresTextInput { get; set; }

        /// <summary>
        /// Prompt a mostrar cuando se requiere texto adicional
        /// </summary>
        [JsonPropertyName("textInputPrompt")]
        public string? TextInputPrompt { get; set; }

        /// <summary>
        /// Placeholder para el campo de texto adicional
        /// </summary>
        [JsonPropertyName("textInputPlaceholder")]
        public string? TextInputPlaceholder { get; set; }
    }
}