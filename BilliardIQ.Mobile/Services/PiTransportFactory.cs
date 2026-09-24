namespace BilliardIQ.Mobile.Services;

internal sealed class PiTransportFactory : IPiTransportFactory
{
    public IPiTransport CreateWebSocket(string uri) => new WebSocketPiTransport(uri);
}
