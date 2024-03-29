﻿using System;
using System.ComponentModel;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ColorKraken;

public class ColorPickerViewModel : ObservableObject
{
    private static TypeConverter TypeConverter { get; } =
    TypeDescriptor.GetConverter(typeof(Color));

    private Color? _color;
    private readonly ThemeColor _themeColor;

    public RelayCommand ApplyCommand { get; }

    public ColorPickerViewModel(ThemeColor themeColor)
    {
        _themeColor = themeColor;

        ApplyCommand = new RelayCommand(OnApply);

        if (themeColor.Value is string value)
        {
            try
            {
                Color = TypeConverter.ConvertFromString(value) as Color?;
            }
            catch(FormatException)
            { }
        }
    }

    private void OnApply()
    {
        if (Color is Color color)
        {
            _themeColor.Value = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }

    public Color? Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }
}

