using System;
using System.Windows;
using System.Windows.Input;

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
    }
}
