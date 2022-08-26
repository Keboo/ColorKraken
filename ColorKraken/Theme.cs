using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

using CommunityToolkit.Mvvm.Messaging;

namespace ColorKraken;

public record class Theme(string Name, string FilePath)
{
    public bool IsDefault => string.Equals(Path.GetExtension(FilePath), ".jsonc-default", System.StringComparison.Ordinal); 
}

public record class ThemeCategory(string Name, IReadOnlyList<ThemeColor> Colors)
{ }

public interface IThemeColorFactory
{
    ThemeColor Create(string name, string? initialValue);
}

public class ThemeColorFactory : IThemeColorFactory
{
    public ThemeColorFactory(IMessenger messenger)
    {
        Messenger = messenger;
    }

    public IMessenger Messenger { get; }

    public ThemeColor Create(string name, string? initialValue)
        => new(name, Messenger) { Value = initialValue };
}

public delegate ThemeColor CreateTheme(string name, string? initialValue);

public record class ThemeColor(string Name, IMessenger Messenger) : INotifyPropertyChanged
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
                Messenger.Send(new BrushUpdated(this, previousValue));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}