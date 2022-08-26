
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using MaterialDesignThemes.Wpf;

namespace ColorKraken;

public class EditorViewModel : ObservableObject,
    IRecipient<BrushUpdated>, 
    IRecipient<ThemesUpdated>
{
    public IMessenger Messenger { get; }
    public IThemeManager ThemeManager { get; }

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

    public EditorViewModel(
        IMessenger messenger,
        IThemeManager themeManager)
    {
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        ThemeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
        Messenger.Register<BrushUpdated>(this);
        Messenger.Register<ThemesUpdated>(this);

        NewThemeCommand = new AsyncRelayCommand(NewTheme);
        OpenThemeFolderCommand = new RelayCommand(OnOpenThemeFolder);
        DeleteCommand = new RelayCommand(OnDelete, () => SelectedTheme != null && SelectedTheme.IsDefault == false);
        RefreshCommand = new AsyncRelayCommand(OnRefresh);

        BindingOperations.EnableCollectionSynchronization(Themes, new object());
    }

    private void OnOpenThemeFolder()
    {
        ThemeManager.OpenThemeDirectory();
    }

    private async Task LoadThemes()
    {
        await Task.Delay(100);
        Themes.Clear();
        try
        {
            await foreach (Theme theme in ThemeManager.GetThemes())
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
                await foreach (ThemeCategory category in ThemeManager.GetCategories(value))
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
            Theme theme;
            try
            {
                theme = await ThemeManager.CreateTheme(content.Name, baseTheme);
            }
            catch(Exception e)
            {
                ShowError("Error creating new theme", e.ToString());
                return;
            }

            Themes.Add(theme);
            SelectedTheme = theme;

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
                await ThemeManager.SaveTheme(selectedTheme, ThemeCategories ?? Enumerable.Empty<ThemeCategory>());
            });
        }

        await ThemeManager.SaveTheme(selectedTheme, ThemeCategories ?? Enumerable.Empty<ThemeCategory>());
    }

    private void ShowError(string message, string details)
        => Messenger.Send(new ShowError(message, details));

    private void OnDelete()
    {
        //TODO: prompt
        //TODO: Move IsDefault check into manager
        if (SelectedTheme is { } selectedTheme && selectedTheme.IsDefault == false)
        {
            try
            {
                ThemeManager.DeleteTheme(selectedTheme);    
            }
            catch(Exception e)
            {
                ShowError($"Error deleting {selectedTheme.Name}", e.ToString());
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

    public async void Receive(ThemesUpdated message) => await OnRefresh();
}
