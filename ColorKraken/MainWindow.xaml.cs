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
        private EditorViewModel EditorViewModel { get; set; }
        private DownloadViewModel DownloadViewModel { get; set; }

        public MainWindow(EditorViewModel editorViewModel, DownloadViewModel downloadViewModel)
        {
            if (editorViewModel is null)
            {
                throw new ArgumentNullException(nameof(editorViewModel));
            }

            InitializeComponent();
            EditorTab.DataContext = EditorViewModel = editorViewModel;
            DownloadTab.DataContext = DownloadViewModel = downloadViewModel;
            
            TabControl.SelectionChanged += TabControl_SelectionChanged;
            Loaded += MainWindow_Loaded;
        }

        private async void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Contains(DownloadTab))
            {
                TabControl.SelectionChanged -= TabControl_SelectionChanged;
                await DownloadViewModel.DownloadThemes();
            }
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            EditorViewModel.RefreshCommand.Execute(null);
        }

        private async void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            await EditorViewModel.Undo();
        }
    }
}
