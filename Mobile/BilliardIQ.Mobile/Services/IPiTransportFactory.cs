namespace BilliardIQ.Mobile.Services;

public interface IPiTransportFactory
{
    IPiTransport CreateWebSocket(string uri);
}
