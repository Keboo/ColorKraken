using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging;

using Moq;
using Moq.AutoMock;

using Xunit;

namespace ColorKraken.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public async Task OnRefresh_ClearAndAssignsSelectedTheme()
    {
        //Arrange
        AutoMocker mocker = new();
        Mock<IThemeManager> themeManager = mocker.GetMock<IThemeManager>();
        themeManager.Setup(x => x.GetThemes())
            .ReturnsAsyncEnumerable(new Theme("", ""));
        EditorViewModel vm = mocker.CreateInstance<EditorViewModel>();

        vm.SelectedTheme = new Theme("test", "C:\\fakepath.jsonc");

        List<Theme?> propertyValues = new();
        vm.PropertyChanged += OnPropertyChanged;
        void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorViewModel.SelectedTheme))
            {
                propertyValues.Add(vm.SelectedTheme);
            }
        }

        //Act
        await vm.OnRefresh();

        //Assert
        Assert.Equal(2, propertyValues.Count);
        Assert.Null(propertyValues[0]);
        Assert.NotNull(propertyValues[1]);
        Assert.Contains(propertyValues[1], vm.Themes);
    }

    [Fact]
    public async Task RefreshCommand_OnExecute_LoadThemes()
    {
        //ViewModel.RefreshCommand.Execute(null);
        //Arrange
        AutoMocker mocker = new();
        Mock<IThemeManager> themeManager = mocker.GetMock<IThemeManager>();
        themeManager.Setup(x => x.GetThemes())
            .ReturnsAsyncEnumerable(new Theme("", ""));

        EditorViewModel vm = mocker.CreateInstance<EditorViewModel>();

        CancellationTokenSource cts = new();
        TaskCompletionSource tcs = new();
        vm.PropertyChanged += OnPropertyChanged;
        void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorViewModel.SelectedTheme)
                && vm.SelectedTheme is not null)
            {
                tcs.TrySetResult();
            }
        }

        cts.CancelAfter(TimeSpan.FromSeconds(1));
        cts.Token.Register(() => tcs.TrySetCanceled());

        //Act
        vm.RefreshCommand.Execute(null);
        await tcs.Task;

        //Assert
        Assert.True(vm.Themes.Any());

    }

    [Fact]
    public void OpenThemeFolderCommand_StartsExplorerProcess()
    {
        //Arrange
        AutoMocker mocker = new();
        Mock<IThemeManager> themeManagerMock = mocker.GetMock<IThemeManager>();
        EditorViewModel vm = mocker.CreateInstance<EditorViewModel>();

        //Act
        vm.OpenThemeFolderCommand.Execute(null);

        //Assert
        themeManagerMock.Verify(x => x.OpenThemeDirectory());
    }

    [Fact]
    public async void OnReceive_BrushUpdatedMessage_UpdatesTheme()
    {
        //Arrange
        AutoMocker mocker = new();
        WeakReferenceMessenger messenger = new();
        mocker.Use<IMessenger>(messenger);

        string filePath = TestFileHelper.CreateTestFile();
        Theme selectedTheme = new("Test Theme", filePath);

        Mock<IThemeManager> themeManager = mocker.GetMock<IThemeManager>();
        themeManager.Setup(x => x.SaveTheme(selectedTheme, It.IsAny<IEnumerable<ThemeCategory>>()));
        themeManager.Setup(x => x.GetCategories(It.IsAny<Theme>())).ReturnsAsyncEnumerable();
        EditorViewModel vm = mocker.CreateInstance<EditorViewModel>();

        var categoriesSet = vm.WatchPropertyChanges<List<ThemeCategory>?>(nameof(EditorViewModel.ThemeCategories));
        vm.SelectedTheme = selectedTheme;

        await categoriesSet.WaitForChange();

        var color = new ThemeColor("TestColor", messenger) { Value = "new" };
        var message = new BrushUpdated(color, "old");

        //Act
        messenger.Send(message);

        //Assert
        themeManager.VerifyAll();
    }
}
