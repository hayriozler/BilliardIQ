namespace BilliardIQ.Mobile.Services;

public interface IPiTransport : IDisposable
{
    event EventHandler<string>? MessageReceived;
    event EventHandler<Exception>? Faulted;

    Task ConnectAsync(CancellationToken cancellationToken);
    Task DisconnectAsync();
    Task SendMessageAsync(string message);
}
