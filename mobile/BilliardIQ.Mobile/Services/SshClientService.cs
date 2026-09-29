using Renci.SshNet;

namespace BillardIQ.Mobile.Services;

public class SshClientService : ISshClientService
{
    // One-shot connection per command — this is a simple command runner, not an interactive
    // session, so there's no benefit to keeping a client open between calls.
    public Task<string> RunCommandAsync(string host, int port, string username, string password, string command) =>
        Task.Run(() =>
        {
            using var client = new SshClient(host, port, username, password);
            client.Connect();
            try
            {
                using var sshCommand = client.CreateCommand(command);
                var result = sshCommand.Execute();
                return string.IsNullOrEmpty(sshCommand.Error) ? result : $"{result}{Environment.NewLine}{sshCommand.Error}";
            }
            finally
            {
                client.Disconnect();
            }
        });
}
