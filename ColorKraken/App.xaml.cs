using System;
using System.Windows;
using System.Windows.Threading;

using MaterialDesignThemes.Wpf;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace ColorKraken
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        public static void Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();
            host.Start();

            //Parallel.ForEach(Enumerable.Repeat(0, 100), (_) =>
            //{
            //    host.Services.GetRequiredService<MainWindowViewModel>();
            //});

            App app = new();
            app.InitializeComponent();
            app.MainWindow = host.Services.GetRequiredService<MainWindow>();
            app.MainWindow.Visibility = Visibility.Visible;
            app.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton<IProcessService, ProcessService>();
                    services.AddSingleton<IThemeManager, ThemeManager>();

                    services.AddSingleton<WeakReferenceMessenger>();
                    services.AddSingleton<IMessenger, WeakReferenceMessenger>(provider => provider.GetRequiredService<WeakReferenceMessenger>());

                    //services.AddSingleton<IThemeColorFactory, ThemeColorFactory>();
                    services.AddSingleton<CreateTheme>(provider => 
                        new CreateTheme(
                            (string name, string? initValue) => 
                                new ThemeColor(name, provider.GetRequiredService<IMessenger>()) 
                                { 
                                    Value = initValue
                                })
                        );

                    services.AddSingleton<Dispatcher>(_ => Current.Dispatcher);

                    services.AddTransient<ISnackbarMessageQueue>(provider =>
                    {
                        Dispatcher dispatcher = provider.GetRequiredService<Dispatcher>();
                        return new SnackbarMessageQueue(TimeSpan.FromSeconds(3.0), dispatcher);
                    });
                });
    }

    public class MyDispatcher : IDispatcher
    {
        public MyDispatcher(Dispatcher dispatcher)
        {
            Dispatcher = dispatcher;
        }

        public Dispatcher Dispatcher { get; }

        public void Invoke(Action action) => Dispatcher.Invoke(action);
    }

    public interface IDispatcher
    {
        void Invoke(Action action);
    }
}
