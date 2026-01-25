using System.Globalization;

namespace IFit.Converters
{
    /// <summary>
    /// Convertidor que invierte un valor booleano.
    /// Útil para mostrar/ocultar elementos basándose en el estado opuesto.
    /// 
    /// Ejemplo:
    /// IsVisible="{Binding IsLoading, Converter={StaticResource InvertedBoolConverter}}"
    /// Si IsLoading = true, entonces IsVisible = false (oculto)
    /// Si IsLoading = false, entonces IsVisible = true (visible)
    /// </summary>
    public class InvertedBoolConverter : IValueConverter
    {
        /// <summary>
        /// Convierte un booleano a su valor inverso
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;  // Invertir el valor
            }

            return false;  // Valor por defecto si no es booleano
        }

        /// <summary>
        /// Convierte de vuelta (no se usa generalmente, pero debe estar implementado)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;  // Invertir el valor
            }

            return false;
        }
    }
}