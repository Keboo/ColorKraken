using System.Threading.Tasks;

using Xunit;
using ColorKraken;
using Moq;
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.Messaging;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using Moq.AutoMock;
using System.Diagnostics;
using System.IO;

namespace ColorKraken.Tests
{

    public class MainWindowViewModelTests
    {
        [Fact]
        public async Task OnRefresh_ClearAndAssignsSelectedTheme()
        {
            //Arrange
            AutoMocker mocker = new();
            MainWindowViewModel vm = mocker.CreateInstance<MainWindowViewModel>();
            //new(messageQueue.Object, messenger.Object, (_, _)  => null!);

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

            cts.CancelAfter(TimeSpan.FromSeconds(5));
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
            Mock<IProcessService> processServiceMock = mocker.GetMock<IProcessService>();
            MainWindowViewModel vm = mocker.CreateInstance<MainWindowViewModel>();

            //Act
            vm.OpenThemeFolderCommand.Execute(null);

            //Assert
            processServiceMock.Verify(x => x.Start(
                It.Is<ProcessStartInfo>(startInfo => startInfo.FileName.Contains(".gitkraken"))));
        }

        [Fact]
        public async Task OnReceive_BrushUpdatedMessage_UpdateThemeFile()
        {
            //Arrange
            AutoMocker mocker = new();
            WeakReferenceMessenger messenger = new();
            mocker.Use<IMessenger>(messenger);
            mocker.Use<CreateTheme>((string name, string? initValue) =>
                                new ThemeColor(name, messenger)
                                {
                                    Value = initValue
                                });

            MainWindowViewModel vm = mocker.CreateInstance<MainWindowViewModel>();

            string filePath = TestFileHelper.CreateTestFile();
            Theme selectedTheme = new("Test Theme", filePath);

            var waiter = new TaskCompletionSource<ThemeColor>();

            vm.PropertyChanged += OnPropertyChanged;
            void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(MainWindowViewModel.ThemeCategories) &&
                    vm.SelectedTheme is not null)
                {
                    var color = vm.ThemeCategories!.First().Colors.First();
                    waiter.TrySetResult(color);
                }
            }

            vm.SelectedTheme = selectedTheme;

            ThemeColor themeColor = await waiter.Task;
            string? previousValue = themeColor.Value;
            themeColor.Value += "new";

            var message = new BrushUpdated(themeColor, previousValue);

            //Act
            messenger.Send(message);

            //Assert
            //Force reload of theme
            vm.SelectedTheme = null;
            waiter = new TaskCompletionSource<ThemeColor>();
            vm.SelectedTheme = selectedTheme;
            ThemeColor updatedColor = await waiter.Task;

            Assert.Equal(themeColor.Name, updatedColor.Name);
            Assert.Equal(themeColor.Value, updatedColor.Value);
        }
    }
}