using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ColorKraken;

public interface IThemeManager
{
    IAsyncEnumerable<Theme> GetThemes();
    IAsyncEnumerable<ThemeCategory> GetCategories(Theme theme);
    Task<Theme> CreateTheme(string name, Theme baseTheme);
    Task SaveTheme(Theme theme, IEnumerable<ThemeCategory> themeCategories);
    void OpenThemeDirectory();
    Task DeleteTheme(Theme theme);
}


public class ThemeManager : IThemeManager
{
    private static JsonSerializerOptions JsonReadOptions { get; } = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static JsonSerializerOptions JsonWriteOptions { get; } = new()
    {
        WriteIndented = true
    };

    public CreateTheme CreateThemeFactory { get; }
    private IProcessService ProcessService { get; }

    private static string GetThemesDirectoryPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".gitkraken", "themes");

    public ThemeManager(IProcessService processService, CreateTheme createThemeFactory)
    {
        ProcessService = processService ?? throw new ArgumentNullException(nameof(processService));
        CreateThemeFactory = createThemeFactory ?? throw new ArgumentNullException(nameof(createThemeFactory));
    }

    public void OpenThemeDirectory()
    {
        ProcessService.Start(new ProcessStartInfo
        {
            FileName = GetThemesDirectoryPath(),
            UseShellExecute = true
        });
    }

    public async Task<Theme> CreateTheme(string name, Theme baseTheme)
    {
        string filePath = name;
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            filePath = filePath.Replace(c, '_');
        }

        filePath += ".jsonc";
        filePath = Path.Combine(GetThemesDirectoryPath(), filePath);

        //TODO: Make sure new name does not already exist.
        //TODO: Make sure new does not math source.
        File.Copy(baseTheme.FilePath, filePath, true);
        //The default files are marked as readonly. Need to clear the flag.
        File.SetAttributes(filePath, FileAttributes.Normal);

        Theme theme = baseTheme with
        {
            FilePath = filePath,
            Name = name
        };
        await WriteTheme(theme, jsonObject =>
        {
            JsonNode meta = jsonObject["meta"] ??= new JsonObject();
            meta["name"] = name;
        });
        return theme;
    }

    public async IAsyncEnumerable<Theme> GetThemes()
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

    public async IAsyncEnumerable<ThemeCategory> GetCategories(Theme theme)
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

    public async Task SaveTheme(Theme theme, IEnumerable<ThemeCategory> themeCategories)
    {
        await WriteTheme(theme, jsonObject =>
        {
            JsonObject themeValues = new();

            foreach (ThemeCategory category in themeCategories)
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

    private static async Task WriteTheme(Theme theme, Action<JsonObject> applyThemeChanges)
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
            throw new Exception($"Error writing theme file '{Path.GetFileName(theme.FilePath)}'", e);
        }
    }

    public Task DeleteTheme(Theme theme)
    {
        File.Delete(theme.FilePath);
        return Task.CompletedTask;
    }

    private static async Task<Theme?> ReadThemeAsync(string filePath)
    {
        JsonObject? jsonObject;
        try
        {
            using Stream fileStream = File.OpenRead(filePath);
            jsonObject = await JsonSerializer.DeserializeAsync<JsonObject>(fileStream, JsonReadOptions);
        }
        catch (Exception e)
        {
            throw new Exception($"Error reading theme file {filePath}", e);
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
}
