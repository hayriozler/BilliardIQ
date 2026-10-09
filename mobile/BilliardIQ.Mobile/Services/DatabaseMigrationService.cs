using Microsoft.Data.Sqlite;

namespace BilliardIQ.Mobile.Services;


internal sealed class DatabaseMigrationService(DatabaseExecutor dbExecutor)
{
    internal abstract class BaseDatabaseMigrationService
    {
        //internal static void DropDatabaseFileIfExists()
        //{
        //    try
        //    {
        //        var fileName = $"{FileSystem.AppDataDirectory}/{Constants.DatabaseFileName}";
        //        if (File.Exists(fileName))
        //        {
        //            File.Delete(fileName);
        //            Console.WriteLine("Old database deleted.");
        //        }

        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        internal abstract Task RunAsync();
    }
    private readonly BaseDatabaseMigrationService[] _migrationServices = [
        new GameTableMigrationService(dbExecutor),
        new ScoreboardTableMigrationService(dbExecutor),
        new MatchResultTableMigrationService(dbExecutor),
        new SshConnectionTableMigrationService(dbExecutor),
    ];

    private static async Task InitializeDatabaseAsync()
    {
        //if (Preferences.Get("DropDatabaseOnStartup", false))
        //    BaseDatabaseMigrationService.DropDatabaseFileIfExists();
        var connection = DatabaseExecutor.GetNewDbConnection();
        await connection.OpenAsync();
        await connection.DisposeAsync();
    }
    public async Task RunMigrationAsync()
    {
        await InitializeDatabaseAsync();
        foreach (var migration in _migrationServices)
        {
            await migration.RunAsync();
        }
    }
    private sealed class GameTableMigrationService(DatabaseExecutor dbExecutor) : BaseDatabaseMigrationService
    {
        //Opponent
        readonly string _tableCreationSql = @"
            CREATE TABLE IF NOT EXISTS Games (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OpponentName TEXT NOT NULL,
                Ball TEXT NOT NULL,
                Location TEXT,
                Date DateTime NOT NULL,
                PlayerScore INTEGER NOT NULL DEFAULT 0,
                OpponentScore INTEGER NOT NULL DEFAULT 0,
                HighestRun INTEGER NOT NULL DEFAULT 0,
                Innings REAL NOT NULL DEFAULT 0,
                Notes TEXT,
                ScoreboardThumbnail BLOB,
                CreatedAt DateTime NOT NULL
            );
CREATE TABLE IF NOT EXISTS GamePhotos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                GameId INTEGER NOT NULL,
                PhotoName TEXT NOT NULL,
                PhotoPath TEXT NOT NULL,
                CreatedAt DateTime NOT NULL
            );
CREATE TABLE IF NOT EXISTS GameStats (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                GameId INTEGER NOT NULL,
                PlayerTotal INT NOT NULL,
                OpponentTotal INT NOT NULL
            );
CREATE TABLE IF NOT EXISTS PlayerStats (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TotalMatches INTEGER NOT NULL DEFAULT 0,
                TotalScore INTEGER NOT NULL DEFAULT 0,
                TotalInnings REAL NOT NULL DEFAULT 0,
                Average REAL NOT NULL DEFAULT 0,
                BestAverage REAL NOT NULL DEFAULT 0,
                HighestRun INTEGER NOT NULL DEFAULT 0,
                UpdatedAt DateTime NOT NULL
            );
";

        internal override async Task RunAsync()
        {
            await dbExecutor.ExecuteAsync(_tableCreationSql);

            if (!await ColumnExistsAsync("Games", "RemoteId"))
                await dbExecutor.ExecuteAsync("ALTER TABLE Games ADD COLUMN RemoteId INTEGER;");
        }

        private static async Task<bool> ColumnExistsAsync(string table, string column)
        {
            using var connection = DatabaseExecutor.GetNewDbConnection();
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name='{column}'";
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
        }
    }

    private sealed class ScoreboardTableMigrationService(DatabaseExecutor dbExecutor) : BaseDatabaseMigrationService
    {
        private readonly string _tableCreationSql = @"
CREATE TABLE IF NOT EXISTS Teams (
    Id INTEGER PRIMARY KEY,
    RemoteId INTEGER,
    Name TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS ScoreboardPlayers (
    Id INTEGER PRIMARY KEY,
    RemoteId INTEGER,
    NickName TEXT NOT NULL,
    Name TEXT NOT NULL,
    Photo BLOB,
    AvatarKey TEXT,
    TeamId INTEGER NOT NULL DEFAULT 0,
    ShortcutNumber INTEGER
);";

        internal override async Task RunAsync()
        {
            await dbExecutor.ExecuteAsync(_tableCreationSql);

            if (!await ColumnExistsAsync("ScoreboardPlayers", "ShortcutNumber"))
                await dbExecutor.ExecuteAsync("ALTER TABLE ScoreboardPlayers ADD COLUMN ShortcutNumber INTEGER;");

            if (!await ColumnExistsAsync("ScoreboardPlayers", "RemoteId"))
                await dbExecutor.ExecuteAsync("ALTER TABLE ScoreboardPlayers ADD COLUMN RemoteId INTEGER;");

            if (!await ColumnExistsAsync("Teams", "RemoteId"))
                await dbExecutor.ExecuteAsync("ALTER TABLE Teams ADD COLUMN RemoteId INTEGER;");
        }

        private static async Task<bool> ColumnExistsAsync(string table, string column)
        {
            using var connection = DatabaseExecutor.GetNewDbConnection();
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name='{column}'";
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
        }
    }

    private sealed class MatchResultTableMigrationService(DatabaseExecutor dbExecutor) : BaseDatabaseMigrationService
    {
        private readonly string _tableCreationSql = @"
CREATE TABLE IF NOT EXISTS match_result (
    Id INTEGER PRIMARY KEY,
    PlayedAt TEXT NOT NULL,
    StartedAt TEXT NULL,
    EndedAt TEXT NULL,
    Player1Id INTEGER NULL,
    Player1Name TEXT NOT NULL DEFAULT '',
    Player1Score INTEGER NOT NULL,
    Player1Avg REAL NOT NULL,
    Player1HighRun INTEGER NOT NULL,
    Player2Id INTEGER NULL,
    Player2Name TEXT NOT NULL DEFAULT '',
    Player2Score INTEGER NOT NULL,
    Player2Avg REAL NOT NULL,
    Player2HighRun INTEGER NOT NULL,
    Inning INTEGER NOT NULL,
    MatchTarget INTEGER NOT NULL,
    Winner INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS match_score_stat (
    Id INTEGER PRIMARY KEY,
    MatchResultId INTEGER NOT NULL,
    PlayerSlot INTEGER NOT NULL,
    BucketIndex INTEGER NOT NULL,
    TotalPoints INTEGER NOT NULL
);";

        internal override async Task RunAsync()
        {
            await dbExecutor.ExecuteAsync(_tableCreationSql);

            if (!await ColumnExistsAsync("match_result", "ScoreDistributionBucketMinutes"))
                await dbExecutor.ExecuteAsync("ALTER TABLE match_result ADD COLUMN ScoreDistributionBucketMinutes INTEGER NOT NULL DEFAULT 5;");
        }

        private static async Task<bool> ColumnExistsAsync(string table, string column)
        {
            using var connection = DatabaseExecutor.GetNewDbConnection();
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name='{column}'";
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
        }
    }

    // Password isn't stored here — see SshConsolePageModel, which keeps it in SecureStorage instead.
    private sealed class SshConnectionTableMigrationService(DatabaseExecutor dbExecutor) : BaseDatabaseMigrationService
    {
        private readonly string _tableCreationSql = @"
CREATE TABLE IF NOT EXISTS SshConnection (
    Id INTEGER PRIMARY KEY,
    Host TEXT NOT NULL DEFAULT '',
    Port INTEGER NOT NULL DEFAULT 22,
    Username TEXT NOT NULL DEFAULT ''
);";

        internal override async Task RunAsync() => await dbExecutor.ExecuteAsync(_tableCreationSql);
    }
}