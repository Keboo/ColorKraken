using System;
using System.Globalization;
using System.Windows.Data;

namespace ColorKraken;

public class PickerViewModelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ThemeColor themeColor)
        {
            return new ColorPickerViewModel(themeColor);
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

