namespace BilliardIQ.Mobile.Services;

public interface ISshClientService
{
    Task<string> RunCommandAsync(string host, int port, string username, string password, string command);
}
