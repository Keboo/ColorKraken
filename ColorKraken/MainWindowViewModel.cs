
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    public IMessenger Messenger { get; }
    public IThemeManager ThemeManager { get; }

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
        IThemeManager themeManager)
    {
        MessageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        ThemeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
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
        => MessageQueue.Enqueue(message, "Details", OnShowErrorDetails, details);

    private async void OnShowErrorDetails(string? details)
    {
        if (string.IsNullOrEmpty(details)) return;
        await DialogHost.Show(new ErrorDetailsViewModel(details), "Root");
    }

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
}

public record class ErrorDetailsViewModel(string Details) { }
