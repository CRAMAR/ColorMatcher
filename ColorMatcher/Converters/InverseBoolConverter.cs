using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ColorMatcher.Converters
{
    /// <summary>
    /// Converts a boolean value to its inverse (true becomes false, false becomes true).
    /// Used for UI bindings where opposite logic is needed, such as enabling a Connect button
    /// only when sensor is NOT connected.
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to its inverse.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">Culture information (not used).</param>
        /// <returns>The inverse of the input boolean value.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        /// <summary>
        /// Converts a boolean value back to its inverse (for two-way binding).
        /// </summary>
        /// <param name="value">The boolean value to convert back.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">Culture information (not used).</param>
        /// <returns>The inverse of the input boolean value.</returns>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }
}
