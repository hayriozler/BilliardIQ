using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BilliardIQ.Mobile.Data;

public class PlayerRepository(ILogger<PlayerRepository> Logger, DatabaseExecutor dbExecutor) : BaseRepo
{
    private async Task<int> UpsertPlayerAsync(Player player)
    {
        var currentDbPlayer = await GetPlayerAsync();
        List<SqliteParameter> parameters = [];
        string query = @"INSERT INTO Player(Name, LastName, Level, Country, City, Club, Association, Email, Phone, CreatedAt) VALUES (@Name, @LastName, @Level, @Country, @City, @Club, @Association, @Email, @Phone, @CreatedAt)";
        if (currentDbPlayer is null)
        {
            parameters.Add(new SqliteParameter("@CreatedAt", DateTime.UtcNow));
        }
        else
        {
            query = "UPDATE Player SET Name = @Name, LastName = @LastName, Level = @Level, Country = @Country, City = @City, Club = @Club, Association = @Association, Email = @Email, Phone = @Phone";
            parameters.Add(new SqliteParameter("@Id", player.Id));
        }
        parameters.Add(new SqliteParameter("@Name", player.Name));
        parameters.Add(new SqliteParameter("@LastName", player.LastName));
        parameters.Add(new SqliteParameter("@Level", player.Level));
        parameters.Add(new SqliteParameter("@Country", player.Country));
        parameters.Add(new SqliteParameter("@City", player.City));
        parameters.Add(new SqliteParameter("@Club", (object?)player.Club ?? DBNull.Value));
        parameters.Add(new SqliteParameter("@Association", (object?)player.Association ?? DBNull.Value));
        parameters.Add(new SqliteParameter("@Email", player.Email));
        parameters.Add(new SqliteParameter("@Phone", (object?)player.Phone ?? DBNull.Value));
        return await dbExecutor.ExecuteAsync(query, parameters);
    }

    public async Task UpsertAsync(Player player)
    {
        var affectedRow = await UpsertPlayerAsync(player);
        if (affectedRow > 0)
        {
            Logger.LogInformation("Player has been upserted successfully");
        }
        else
        {
            Logger.LogDebug("Something went wrong....");
        }
    }

    public async Task<Player?> GetPlayerAsync()
    {
        var player = await dbExecutor.ReadSingleDataAsync<Player>("select * from Player", []);
        if (player is null)
        {
            return null;
        }
        return player;
    }
}
