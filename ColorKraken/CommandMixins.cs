using System.Windows;

using Microsoft.Toolkit.Mvvm.Input;

namespace ColorKraken;

public static class CommandMixins
{
    public static void RaiseCanExecuteChanged(this IRelayCommand command)
    {
        Application.Current.Dispatcher.Invoke(() => command.NotifyCanExecuteChanged());
    }
}
