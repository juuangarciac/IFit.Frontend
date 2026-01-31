using System.Globalization;

namespace IFit.Converters
{
    /// <summary>
    /// Converter que devuelve true si el string no es null ni vacío.
    /// Útil para controlar la visibilidad de elementos basados en si hay texto.
    /// </summary>
    public class StringNotNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return !string.IsNullOrWhiteSpace(stringValue);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
