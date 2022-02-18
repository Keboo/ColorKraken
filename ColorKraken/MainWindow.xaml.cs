using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ColorKraken
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel { get; set; }


        public MainWindow(MainWindowViewModel viewModel)
        {
            if (viewModel is null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            DataContext = ViewModel = viewModel;
            InitializeComponent();
            viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
            Loaded                    += MainWindow_Loaded;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshCommand.Execute(null);
        }

        private async void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            await ViewModel.Undo();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var categories = ViewModel.ThemeCategories?.Select(x => x with { }).ToList();
            if (categories is null) return;

            ThemeColor? color = categories.SelectMany(x => x.Colors).FirstOrDefault(x => x.Name == "app__bg0");
            if (color is null) return;
            ColorAnimation animation = new ColorAnimation(Colors.Blue, new Duration(TimeSpan.FromSeconds(1)));
            animation.AutoReverse = false;

            try
            {
                SolidColorBrush brush = new(Colors.Red);
                List<string> values = new();
                brush.Changed += Brush_Changed;
                animation.Completed += Animation_Completed;
                brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);

                void Brush_Changed(object? sender, EventArgs e)
                {
                    var current = brush.Color;
                    values.Add($"#{current.R:X2}{current.G:X2}{current.B:X2}");
                }

                async void Animation_Completed(object? sender, EventArgs e)
                {
                    foreach (var value in values)
                    {
                        Debug.WriteLine(value);
                        color.Value = value;
                        await ViewModel.ThemeManager.SaveTheme(ViewModel.SelectedTheme!, categories);
                        await Task.Delay(500);
                    }
                }

                
            }
            catch(Exception ex)
            {

            }
        }

    }
}
