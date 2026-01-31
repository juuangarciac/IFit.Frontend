using System.Globalization;

namespace IFit.Converters
{
    /// <summary>
    /// Converter que convierte un bool a un valor de opacidad.
    /// True = 1.0 (opaco), False = 0.3 (semi-transparente)
    /// </summary>
    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 1.0 : 0.3;
            }
            return 0.3;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
