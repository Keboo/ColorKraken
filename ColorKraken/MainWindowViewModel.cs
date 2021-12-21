﻿
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
    private static JsonSerializerOptions JsonReadOptions { get; } = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static JsonSerializerOptions JsonWriteOptions { get; } = new()
    {
        WriteIndented = true
    };

    public static IMessenger Messenger { get; } = new WeakReferenceMessenger();

    public AsyncRelayCommand NewThemeCommand { get; }
    public IRelayCommand OpenThemeFolderCommand { get; }

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
                Task.Run(async () =>
                {
                    await Task.Delay(300); //Let the animations finish before startin
                    await LoadThemeBrushes(value);
                });
            }
        }
    }

    public MainWindowViewModel()
    {
        Messenger.Register(this);

        NewThemeCommand = new AsyncRelayCommand(NewTheme);
        OpenThemeFolderCommand = new RelayCommand(OnOpenThemeFolder);

        BindingOperations.EnableCollectionSynchronization(Themes, new object());

        Task.Run(LoadThemes);
    }

    private void OnOpenThemeFolder()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = GetThemesDirectoryPath(),
            UseShellExecute = true
        });
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
        var content = new NewThemeViewModel(Themes);
        if (await DialogHost.Show(content, "Root") as bool? == true &&
            content.SelectedTheme is { } selectedTheme)
        {
            //TODO: Make sure new name does not already exist.
            string filePath = content.Name;
            //TODO: Sanitize input name
            filePath += ".jsonc";
            filePath = Path.Combine(GetThemesDirectoryPath(), filePath);

            Theme theme = selectedTheme with
            {
                FilePath = filePath,
                Name = content.Name
            };
            await WriteTheme(theme, jsonObject =>
            {
                JsonNode meta = jsonObject["meta"] ??= new JsonObject();
                meta["name"] = content.Name;
            });

            Themes.Add(theme);
            SelectedTheme = theme;

        }

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

    private static string GetThemesDirectoryPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".gitkraken", "themes");

    private static async IAsyncEnumerable<Theme> GetThemes()
    {
        string themeDirectory = GetThemesDirectoryPath();

        JsonSerializerOptions options = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        foreach (string file in Directory.EnumerateFiles(themeDirectory))
        {
            if (await ReadThemeAsync(file) is { } theme)
            {
                yield return theme;
            }
        }
    }

    private static async Task<Theme?> ReadThemeAsync(string filePath)
    {
        JsonObject? jsonObject;
        try
        {
            using Stream fileStream = File.OpenRead(filePath);
            jsonObject = await JsonSerializer.DeserializeAsync<JsonObject>(fileStream, JsonReadOptions);
        }
        catch (Exception _)
        {
            //TODO
            return null;
        }
        if (jsonObject is not null &&
            jsonObject["meta"] is JsonObject metadata &&
            metadata["name"] is JsonValue nameValue &&
            !string.IsNullOrWhiteSpace(nameValue.GetValue<string>()))
        {
            return new Theme(nameValue.GetValue<string>(), filePath);
        }
        return null;
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
                await SaveCurrentAsTheme(selectedTheme);
            });
        }

        await SaveCurrentAsTheme(selectedTheme);
    }

    private async Task SaveCurrentAsTheme(Theme theme)
    {
        await WriteTheme(theme, jsonObject =>
        {
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
        });
    }

    private async Task WriteTheme(Theme theme, Action<JsonObject> applyThemeChanges)
    {
        try
        {
            JsonObject? jsonObject;
            using (Stream readStream = File.OpenRead(theme.FilePath))
            {
                jsonObject = await JsonSerializer.DeserializeAsync<JsonObject>(readStream, JsonReadOptions);
            }
            if (jsonObject is null) return;

            applyThemeChanges(jsonObject);

            using Stream writeStream = File.Open(theme.FilePath, FileMode.Create, FileAccess.Write);
            await JsonSerializer.SerializeAsync(writeStream, jsonObject, JsonWriteOptions);
        }
        catch (Exception _)
        {

        }
    }


}

