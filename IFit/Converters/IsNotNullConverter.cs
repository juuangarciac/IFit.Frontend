using System.Globalization;

namespace IFit.Converters
{
    /// <summary>
    /// Converter que devuelve true si el valor no es null.
    /// Útil para controlar visibilidad de elementos basados en propiedades nullable.
    /// </summary>
    public class IsNotNullConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is not null;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
