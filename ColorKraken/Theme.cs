using System.Collections.Generic;
using System.ComponentModel;

using Microsoft.Toolkit.Mvvm.Messaging;

namespace ColorKraken;

public record class Theme(string Name, string FilePath)
{ }

public record class ThemeCategory(string Name, IReadOnlyList<ThemeColor> Colors)
{ }

public record class ThemeColor(string Name) : INotifyPropertyChanged
{
    private string? _value;
    public string? Value
    {
        get => _value;
        set
        {
            string? previousValue = _value;
            if (_value != value)
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                MainWindowViewModel.Messenger.Send(new BrushUpdated(this, previousValue));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}