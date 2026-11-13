using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;

namespace BilliardIQ.Mobile.Data;

public class SshConnectionRepository(DatabaseExecutor dbExecutor) : BaseRepo
{
    public async Task<SshConnectionSettings?> GetAsync() =>
        await dbExecutor.ReadSingleDataAsync<SshConnectionSettings>("SELECT Host, Port, Username FROM SshConnection WHERE Id = 1");

    public async Task SaveAsync(SshConnectionSettings settings) =>
        await dbExecutor.ExecuteAsync(
            "INSERT OR REPLACE INTO SshConnection(Id, Host, Port, Username) VALUES(1, @Host, @Port, @Username)",
            [new("@Host", settings.Host), new("@Port", settings.Port), new("@Username", settings.Username)]);
}
