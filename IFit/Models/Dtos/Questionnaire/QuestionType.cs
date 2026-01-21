namespace IFit.Models.Dtos.Questionnaire
{
    /// <summary>
    /// Tipo de pregunta en el cuestionario
    /// </summary>
    public enum QuestionType
    {
        /// <summary>
        /// Pregunta de opción múltiple con una sola respuesta
        /// </summary>
        MULTIPLE_CHOICE,

        /// <summary>
        /// Pregunta de texto libre
        /// </summary>
        TEXT_INPUT,

        /// <summary>
        /// Pregunta de sí/no
        /// </summary>
        YES_NO,

        /// <summary>
        /// Pregunta de escala numérica (ej: 1-10)
        /// </summary>
        SCALE,
        NUMERIC
    }
}