
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public IMessenger Messenger { get; }

    //public IThemeColorFactory ThemeColorFactory { get; }
    public CreateTheme CreateThemeFactory { get; }

    public ISnackbarMessageQueue MessageQueue { get; }

    public AsyncRelayCommand NewThemeCommand { get; }
    public IRelayCommand DeleteCommand { get; }
    public IRelayCommand RefreshCommand { get; }
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
                DeleteCommand.RaiseCanExecuteChanged();
                UndoStack.Clear();
                Task.Run(async () =>
                {
                    await LoadThemeBrushes(value);
                });
            }
        }
    }

    public MainWindowViewModel(
        ISnackbarMessageQueue messageQueue,
        IMessenger messenger,
        IServiceProvider serviceProvider,
        CreateTheme createThemeFactory)
    {
        MessageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        CreateThemeFactory = createThemeFactory;
        //ThemeColorFactory = themeColorFactory ?? throw new ArgumentNullException(nameof(themeColorFactory));
        Messenger.Register(this);

        NewThemeCommand = new AsyncRelayCommand(NewTheme);
        OpenThemeFolderCommand = new RelayCommand(OnOpenThemeFolder);
        DeleteCommand = new RelayCommand(OnDelete, () => SelectedTheme != null && SelectedTheme.IsDefault == false);
        RefreshCommand = new AsyncRelayCommand(OnRefresh);

        BindingOperations.EnableCollectionSynchronization(Themes, new object());
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
        catch (Exception e)
        {
            ShowError("Error loading theme file", e.ToString());
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
            catch (Exception e)
            {
                ShowError("Error loading theme resources", e.ToString());
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
            content.SelectedTheme is { } baseTheme)
        {
            string filePath = content.Name;
            foreach(var c in Path.GetInvalidFileNameChars())
            {
                filePath = filePath.Replace(c, '_');
            }

            filePath += ".jsonc";
            filePath = Path.Combine(GetThemesDirectoryPath(), filePath);

            //TODO: Make sure new name does not already exist.
            //TODO: Make sure new does not math source.
            try
            {
                File.Copy(baseTheme.FilePath, filePath, true);
                //The default files are marked as readonly. Need to clear the flag.
                File.SetAttributes(filePath, FileAttributes.Normal);
            }
            catch(Exception e)
            {
                ShowError("Error reating new theme file", e.ToString());
                return;
            }

            Theme theme = baseTheme with
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

    }

    private async IAsyncEnumerable<ThemeCategory> GetCategories(Theme theme)
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
                    colors.Add(CreateThemeFactory(colorName, value));
                    //colors.Add(ThemeColorFactory.Create(colorName, value));
                    //colors.Add(new ThemeColor(colorName, Messenger) { Value = value });
                }

                yield return new ThemeCategory(name, colors);
            }
        }

    }

    private static string GetThemesDirectoryPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".gitkraken", "themes");

    private async IAsyncEnumerable<Theme> GetThemes()
    {
        string themeDirectory = GetThemesDirectoryPath();

        foreach (string file in Directory.EnumerateFiles(themeDirectory))
        {
            if (Path.GetExtension(file).StartsWith(".jsonc", StringComparison.OrdinalIgnoreCase) &&
                await ReadThemeAsync(file) is { } theme)
            {
                yield return theme;
            }
        }
    }

    private async Task<Theme?> ReadThemeAsync(string filePath)
    {
        JsonObject? jsonObject;
        try
        {
            using Stream fileStream = File.OpenRead(filePath);
            jsonObject = await JsonSerializer.DeserializeAsync<JsonObject>(fileStream, JsonReadOptions);
        }
        catch (Exception e)
        {
            ShowError("Error reading theme file", e.ToString());
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

            using Stream writeStream = File.Open(theme.FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            await JsonSerializer.SerializeAsync(writeStream, jsonObject, JsonWriteOptions);
        }
        catch (Exception e)
        {
            ShowError($"Error writing theme file '{Path.GetFileName(theme.FilePath)}'", e.ToString());
        }
    }

    private void ShowError(string message, string details)
        => MessageQueue.Enqueue(message, "Details", OnShowErrorDetails, details);

    private async void OnShowErrorDetails(string? details)
    {
        if (string.IsNullOrEmpty(details)) return;
        await DialogHost.Show(new ErrorDetailsViewModel(details), "Root");
    }

    private void OnDelete()
    {
        //TODO: prompt
        if (SelectedTheme is { } selectedTheme && selectedTheme.IsDefault == false)
        {
            try
            {
                File.Delete(selectedTheme.FilePath);
            }
            catch(Exception e)
            {
                ShowError($"Error deleting {Path.GetFileName(selectedTheme.FilePath)}", e.ToString());
                return;
            }
            SelectedTheme = null;
            Themes.Remove(selectedTheme);
        }
    }

    public async Task OnRefresh()
    {
        SelectedTheme = null;
        Themes.Clear();
        await Task.Run(LoadThemes);
    }
}

public record class ErrorDetailsViewModel(string Details) { }
