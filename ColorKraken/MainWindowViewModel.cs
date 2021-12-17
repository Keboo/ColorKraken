
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

using MaterialDesignThemes.Wpf;

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace ColorKraken;

public class MainWindowViewModel : ObservableObject, IRecipient<BrushUpdated>
{
    public static IMessenger Messenger { get; } = new WeakReferenceMessenger();

    public AsyncRelayCommand NewThemeCommand { get; }

    public ObservableCollection<Theme> Themes { get; } = new();

    private int _ignoreChanges;

    private List<ThemeCategory>? _themeCategories;
    public List<ThemeCategory>? ThemeCategories
    {
        get => _themeCategories;
        set => SetProperty(ref _themeCategories, value);
    }

    private List<Func<Task>> UndoStack { get; } = new();

    private Theme? _selectedTheme;
    public Theme? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                UndoStack.Clear();
                Task.Run(() => LoadThemeBrushes(value));
            }
        }
    }

    public MainWindowViewModel()
    {
        Messenger.Register(this);

        NewThemeCommand = new AsyncRelayCommand(NewTheme);

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
            SelectedTheme = Themes.Where(x => !x.IsDefault).FirstOrDefault();
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
        if (Interlocked.CompareExchange(ref _ignoreChanges, 1, 0) == 0)
        {
            try
            {
                List<ThemeCategory> categories = new();
                await foreach (ThemeCategory category in GetCategories(value))
                {
                    categories.Add(category);
                }
                ThemeCategories = categories;
            }
            catch (Exception ex)
            {
                //TODO
            }
            Interlocked.Exchange(ref _ignoreChanges, 0);
        }
    }


    public async Task Undo()
    {
        if (UndoStack.Count > 0)
        {
            var item = UndoStack[^1];
            UndoStack.RemoveAt(UndoStack.Count - 1);
            if (Interlocked.CompareExchange(ref _ignoreChanges, 1, 0) == 0)
            {
                try
                {
                    await item();
                }
                finally
                {
                    Interlocked.Exchange(ref _ignoreChanges, 0);
                }
            }
        }
    }

    private async Task NewTheme()
    {
        string? newThemeName = await DialogHost.Show("", "Root") as string;

        //TODO copy existing file
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

        if (_ignoreChanges != 0) return;

        if (!string.IsNullOrWhiteSpace(message.PreviousValue))
        {
            ThemeColor color = message.Color;
            string previousValue = message.PreviousValue;
            UndoStack.Add(async () => 
            {
                color.Value = previousValue;
                await WriteTheme(selectedTheme);
            });
        }

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

        foreach (ThemeCategory category in ThemeCategories ?? Enumerable.Empty<ThemeCategory>())
        {
            JsonObject jsonCategory = new();

            foreach (ThemeColor color in category.Colors)
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

