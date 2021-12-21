using System;
using System.Collections.ObjectModel;

using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace ColorKraken;

public class NewThemeViewModel : ObservableObject
{
    public ObservableCollection<Theme> Themes { get; }

    private Theme? _selectedTheme;
    public Theme? SelectedTheme
    {
        get => _selectedTheme;
        set => SetProperty(ref _selectedTheme, value);
    }

    private string? _name;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public NewThemeViewModel(ObservableCollection<Theme> themes)
    {
        Themes = themes ?? throw new ArgumentNullException(nameof(themes));
    }

}
