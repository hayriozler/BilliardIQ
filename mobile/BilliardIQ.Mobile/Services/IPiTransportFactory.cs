namespace BillardIQ.Mobile.Services;

public interface IPiTransportFactory
{
    IPiTransport CreateWebSocket(string uri);
}
