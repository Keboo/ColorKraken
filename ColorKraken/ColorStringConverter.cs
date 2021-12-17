using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ColorKraken;

public class ColorStringConverter : IValueConverter
{
    private static TypeConverter TypeConverter { get; } =
    TypeDescriptor.GetConverter(typeof(Color));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Color color ? $"#{color.R:X2}{color.G:X2}{color.B:X2}" : value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringColor)
        {
            return TypeConverter.ConvertFromString(stringColor);
        }
        return value;
    }
}

