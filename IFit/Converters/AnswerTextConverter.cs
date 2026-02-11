using System.Globalization;

namespace IFit.Converters
{
    /// <summary>
    /// Converter que devuelve AdditionalText si existe, sino devuelve SelectedOptionText
    /// </summary>
    public class AnswerTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.Dtos.Questionnaire.AnswerDTO answer)
            {
                // Si existe AdditionalText y no está vacío, lo mostramos
                if (!string.IsNullOrWhiteSpace(answer.AdditionalText))
                {
                    return answer.AdditionalText;
                }
                
                // Sino, mostramos el texto de la opción seleccionada
                return answer.SelectedOptionText;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
