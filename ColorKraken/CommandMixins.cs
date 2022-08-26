using System.Windows;

using CommunityToolkit.Mvvm.Input;

namespace ColorKraken;

public static class CommandMixins
{
    public static void RaiseCanExecuteChanged(this IRelayCommand command)
    {
        if (Application.Current is { } app)
        {
            app.Dispatcher.Invoke(() => command.NotifyCanExecuteChanged());
        }
        else
        {
            command.NotifyCanExecuteChanged();
        }
    }
}
