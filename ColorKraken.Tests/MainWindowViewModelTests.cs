using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Toolkit.Mvvm.Messaging;

using Moq;
using Moq.AutoMock;
using Moq.Language.Flow;

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
        MainWindowViewModel vm = mocker.CreateInstance<MainWindowViewModel>();

        vm.SelectedTheme = new Theme("test", "C:\\fakepath.jsonc");

        List<Theme?> propertyValues = new();
        vm.PropertyChanged += OnPropertyChanged;
        void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedTheme))
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

        MainWindowViewModel vm = mocker.CreateInstance<MainWindowViewModel>();

        CancellationTokenSource cts = new();
        TaskCompletionSource tcs = new();
        vm.PropertyChanged += OnPropertyChanged;
        void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedTheme)
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
        MainWindowViewModel vm = mocker.CreateInstance<MainWindowViewModel>();

        //Act
        vm.OpenThemeFolderCommand.Execute(null);

        //Assert
        themeManagerMock.Verify(x => x.OpenThemeDirectory());
    }

    [Fact]
    public void OnReceive_BrushUpdatedMessage_UpdatesTheme()
    {
        //Arrange
        AutoMocker mocker = new();
        WeakReferenceMessenger messenger = new();
        mocker.Use<IMessenger>(messenger);

        string filePath = TestFileHelper.CreateTestFile();
        Theme selectedTheme = new("Test Theme", filePath);

        Mock<IThemeManager> themeManager = mocker.GetMock<IThemeManager>();
        themeManager.Setup(x => x.SaveTheme(selectedTheme, It.IsAny<IEnumerable<ThemeCategory>>()));
        MainWindowViewModel vm = mocker.CreateInstance<MainWindowViewModel>();

        vm.SelectedTheme = selectedTheme;
        var color = new ThemeColor("Testcolor", messenger) { Value = "new" };
        var message = new BrushUpdated(color, "old" );

        //Act
        messenger.Send(message);

        //Assert
        themeManager.VerifyAll();
    }
}
