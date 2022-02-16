using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using ColorKraken.Configuration;

using MaterialDesignThemes.Wpf;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace ColorKraken;

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
            .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
            {
                configurationBuilder.AddUserSecrets(typeof(App).Assembly);
                configurationBuilder.AddAzureKeyVault("https://colorkraken.vault.azure.net/")
                    .IgnoreExceptionFromLastSource();
            })
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

                //services.AddOptions<MySettings>()
                //    .Bind(hostContext.Configuration.GetSection("Settings"));
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
