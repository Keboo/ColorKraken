
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Data;

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace ColorKraken;

public record class BrushUpdated { }

public class MainWindowViewModel : ObservableObject, IRecipient<BrushUpdated>
{
    public static IMessenger Messenger { get; } = new WeakReferenceMessenger();

    public RelayCommand NewThemeCommand { get; }
    public ObservableCollection<Theme> Themes { get; } = new();

    private List<ThemeCategory>? _themeCategories;
    public List<ThemeCategory>? ThemeCategories 
    {
        get => _themeCategories;
        set => SetProperty(ref _themeCategories, value);
    }

    private Theme? _selectedTheme;
    public Theme? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                Task.Run(() => LoadThemeBrushes(value));
            }
        }
    }

    public MainWindowViewModel()
    {
        NewThemeCommand = new RelayCommand(NewTheme);

        BindingOperations.EnableCollectionSynchronization(Themes, new object());

        Task.Run(LoadThemes);
    }

    private async Task LoadThemes()
    {
        Themes.Clear();
        try
        {
            await foreach (Theme theme in GetThemes())
            {
                Themes.Add(theme);
            }
            SelectedTheme = Themes.FirstOrDefault();
        }
        catch (Exception ex)
        {

        }
    }

    private async Task LoadThemeBrushes(Theme? value)
    {
        if (value is null)
        {
            ThemeCategories = null;
            return;
        }
        Messenger.UnregisterAll(this);
        try
        {
            List<ThemeCategory> categories = new();
            await foreach(ThemeCategory category in GetCategories(value))
            {
                categories.Add(category);
            }
            ThemeCategories = categories;
            Messenger.Register(this);
        }
        catch (Exception ex)
        {
            //TODO
        }
    }

    private void NewTheme()
    {
    }

    private static async IAsyncEnumerable<ThemeCategory> GetCategories(Theme theme)
    {
        JsonSerializerOptions options = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        using Stream fileStream = File.OpenRead(theme.FilePath);
        var jsonObject = await JsonSerializer.DeserializeAsync<JsonObject>(fileStream, options);
        
        if (jsonObject?["themeValues"] is JsonObject themeValues)
        {
            foreach ((string name, JsonNode? child) in themeValues)
            {
                if (child is null) continue;
                if (child is not JsonObject childObject) continue;

                List<ThemeColor> colors = new();
                foreach ((string colorName, JsonNode? colorNode) in childObject)
                {
                    string? value = colorNode?.GetValue<string>();
                    colors.Add(new ThemeColor(colorName) { Value = value });
                }

                yield return new ThemeCategory(name, colors);
            }
        }

    }

    private static async IAsyncEnumerable<Theme> GetThemes()
    {
        string themeDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".gitkraken", "themes");

        JsonSerializerOptions options = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        foreach (string file in Directory.EnumerateFiles(themeDirectory))
        {
            JsonObject? jsonObject;
            try
            {
                using Stream fileStream = File.OpenRead(file);
                jsonObject = await JsonSerializer.DeserializeAsync<JsonObject>(fileStream, options);
            }
            catch (Exception _)
            {
                //TODO
                continue;
            }
            if (jsonObject is not null &&
                jsonObject["meta"] is JsonObject metadata &&
                metadata["name"] is JsonValue nameValue &&
                !string.IsNullOrWhiteSpace(nameValue.GetValue<string>()))
            {
                Theme theme = new(nameValue.GetValue<string>(), file);
                yield return theme;
            }
        }
    }

    async void IRecipient<BrushUpdated>.Receive(BrushUpdated message)
    {
        var selectedTheme = SelectedTheme;
        if (selectedTheme is null) return;

        await WriteTheme(selectedTheme);
    }

    private async Task WriteTheme(Theme theme)
    {
        JsonSerializerOptions options = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true
        };
        JsonObject? jsonObject;
        using (Stream readStream = File.OpenRead(theme.FilePath))
        {
            jsonObject = await JsonSerializer.DeserializeAsync<JsonObject>(readStream, options);
        }
        if (jsonObject is null) return;

        JsonObject themeValues = new();

        foreach(ThemeCategory category in ThemeCategories ?? Enumerable.Empty<ThemeCategory>())
        {
            JsonObject jsonCategory = new();

            foreach(ThemeColor color in category.Colors)
            {
                jsonCategory[color.Name] = color.Value;
            }

            themeValues[category.Name] = jsonCategory;
        }

        jsonObject["themeValues"] = themeValues;

        using Stream writeStream = File.Open(theme.FilePath, FileMode.Create);
        await JsonSerializer.SerializeAsync(writeStream, jsonObject, options);
    }
}

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
            if (_value != value)
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                MainWindowViewModel.Messenger.Send(new BrushUpdated());
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

