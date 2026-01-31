using System.Globalization;

namespace IFit.Converters
{
    /// <summary>
    /// Converter que devuelve diferentes textos según un valor booleano.
    /// Parameter format: "TextoSiTrue|TextoSiFalse"
    /// Ejemplo: "Finalizar|Siguiente" -> Si IsLastQuestion=true muestra "Finalizar", sino "Siguiente"
    /// </summary>
    public class BoolToNextButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isTrue && parameter is string parameterString)
            {
                var parts = parameterString.Split('|');
                if (parts.Length == 2)
                {
                    return isTrue ? parts[0] : parts[1];
                }
            }
            return "Siguiente";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
