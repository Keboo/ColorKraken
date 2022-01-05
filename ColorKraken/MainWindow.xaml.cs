using System.Windows;
using System.Windows.Input;

namespace ColorKraken
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel { get; }

        public MainWindow(MainWindowViewModel viewModel)
        {
            DataContext = ViewModel = viewModel;
            InitializeComponent();
        }

        private async void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            await ViewModel.Undo();
        }
    }
}
