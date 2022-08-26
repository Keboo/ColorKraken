using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Data;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ColorKraken;

[ObservableObject]
public partial class DownloadViewModel
{
    public ObservableCollection<ThemeItem> Items { get; } = new();

    private HttpClient HttpClient { get; }
    private IThemeManager ThemeManager { get; }

    public DownloadViewModel(HttpClient httpClient, IThemeManager themeManager)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ThemeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));

        BindingOperations.EnableCollectionSynchronization(Items, new object());
    }

    public async Task DownloadThemes()
    {
        ThemeItem[]? themeItems = await HttpClient.GetFromJsonAsync<ThemeItem[]>(new Uri("https://raw.githubusercontent.com/Keboo/ColorKraken/master/Themes.json"));

        Items.Clear();
        foreach(var item in themeItems ?? Enumerable.Empty<ThemeItem>())
        {
            Items.Add(item);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task Download(ThemeItem themeItem)
    {
        if (themeItem.Title is null || themeItem.ThemeFile is null) return;
        using Stream themeFileStream = await HttpClient.GetStreamAsync(themeItem.ThemeFile);
        await ThemeManager.CreateTheme(themeItem.Title, themeFileStream);
    }

    private static bool CanDownload(ThemeItem themeItem)
        => themeItem.ThemeFile is not null;
}

public record class ThemeItem(
    [property:JsonPropertyName("title")]
    string? Title,
    [property:JsonPropertyName("theme-file")]
    Uri? ThemeFile,
    [property:JsonPropertyName("description")]
    string? Description,
    [property:JsonPropertyName("author")]
    string? Author,
    [property:JsonPropertyName("preview-image")]
    Uri? PreviewImage,
    [property:JsonPropertyName("thumbnail-image")]
    Uri ThumbnailImage);

