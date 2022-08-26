using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using MaterialDesignThemes.Wpf;

namespace ColorKraken;

[ObservableObject]
public partial class MainWindowViewModel : IRecipient<ShowError>
{
    public ISnackbarMessageQueue MessageQueue { get; }

    public MainWindowViewModel(ISnackbarMessageQueue messageQueue, IMessenger messenger)
    {
        MessageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        messenger.Register(this);
    }


    private async void OnShowErrorDetails(string? details)
    {
        if (string.IsNullOrEmpty(details)) return;
        await DialogHost.Show(new ErrorDetailsViewModel(details), "Root");
    }

    public void Receive(ShowError message)
        => MessageQueue.Enqueue(message.Message, "Details", OnShowErrorDetails, message.Details);
}

public record class ErrorDetailsViewModel(string Details);

public record class ShowError(string Message, string Details);