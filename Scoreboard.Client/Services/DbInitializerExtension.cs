using Microsoft.EntityFrameworkCore;

namespace Scoreboard.Client.Services;

public static class DbInitializerExtension
{
    public static IServiceProvider InitializeDbAsync(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw("""
           CREATE TABLE IF NOT EXISTS Settings (
           Id TEXT PRIMARY KEY,
           Value TEXT NOT NULL)
         """);
        db.Database.ExecuteSqlRaw("""         
         INSERT INTO Settings (Id, Value)
         VALUES ('Lang', 'tr')
                ON CONFLICT(Id) DO NOTHING;
        """
        );
        db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS scoreboard_state (
            Id INTEGER PRIMARY KEY,
            Player1Name TEXT NOT NULL DEFAULT '',
            Player2Name TEXT NOT NULL DEFAULT '',
            Player1Id INTEGER NOT NULL DEFAULT 1,
            Player2Id INTEGER NOT NULL DEFAULT 2,
            Player1Score INTEGER NOT NULL,
            Player2Score INTEGER NOT NULL,
            Inning INTEGER NOT NULL,
            MatchTarget INTEGER NOT NULL,
            Player1Avg REAL NOT NULL,
            Player1HighRun INTEGER NOT NULL,
            CurrentPoints INTEGER NOT NULL,
            Player2Avg REAL NOT NULL,
            Player2HighRun INTEGER NOT NULL,
            ShotClockSeconds INTEGER NOT NULL,
            ShotClockRemaining REAL NOT NULL,
            ShotClockActive INTEGER NOT NULL,
            ActivePlayer INTEGER NOT NULL
        );
        """);

        db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS player (
            Id INTEGER PRIMARY KEY,
            RemoteId INTEGER NULL,
            Nickname TEXT NOT NULL,
            Name TEXT NOT NULL,
            ShortcutNumber INTEGER NULL,
            PhotoPath TEXT NULL,
            AvatarId INTEGER NULL,
            TeamId INTEGER NULL,
            Level INTEGER NULL,
            Country TEXT NULL,
            City TEXT NULL,
            LicenseNo TEXT NULL,
            LicenseValidUntil TEXT NULL,
            AssociationName TEXT NULL,
            UpdatedAt TEXT NULL,
            IsSystem INTEGER NOT NULL DEFAULT 0,
            SystemSlot INTEGER NULL
        );
        """);

        db.Database.ExecuteSqlRaw("""         
         INSERT INTO player (Id, Nickname, Name)
         VALUES (1, 'P1', 'Player 1'),
                (2, 'P2', 'Player 2')
                ON CONFLICT(Id) DO NOTHING;
        """);

        db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS match_result (
            Id INTEGER PRIMARY KEY,
            PlayedAt TEXT NOT NULL,
            StartedAt TEXT NULL,
            EndedAt TEXT NULL,
            Player1Id INTEGER NOT NULL DEFAULT 1,
            Player1Name TEXT NOT NULL DEFAULT '',
            Player1Score INTEGER NOT NULL,
            Player1Avg REAL NOT NULL,
            Player1HighRun INTEGER NOT NULL,
            Player2Id INTEGER NOT NULL DEFAULT 2,
            Player2Name TEXT NOT NULL DEFAULT '',
            Player2Score INTEGER NOT NULL,
            Player2Avg REAL NOT NULL,
            Player2HighRun INTEGER NOT NULL,
            Inning INTEGER NOT NULL,
            MatchTarget INTEGER NOT NULL,
            Winner INTEGER NOT NULL,
            SyncedAPI INTEGER NOT NULL DEFAULT 0
        );
        """);

        db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS club (
            Id INTEGER PRIMARY KEY,
            RemoteId INTEGER NULL,
            Name TEXT NOT NULL,
            ShortName TEXT NOT NULL DEFAULT '',
            City TEXT NULL,
            PrimaryColor TEXT NULL
        );
        """);

        db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS team (
            Id INTEGER PRIMARY KEY,
            RemoteId INTEGER NULL,
            ClubId INTEGER NULL,
            Name TEXT NOT NULL,
            UpdatedAt TEXT NULL
        );
        """);

        db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS score_event (
            Id INTEGER PRIMARY KEY,
            Timestamp TEXT NOT NULL,
            PlayerSlot INTEGER NOT NULL,
            Points INTEGER NOT NULL,
            StartAt TEXT NULL,
            EndAt TEXT NULL
        );
        """);

        db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS match_score_stat (
            Id INTEGER PRIMARY KEY,
            MatchResultId INTEGER NOT NULL,
            PlayerSlot INTEGER NOT NULL,
            BucketIndex INTEGER NOT NULL,
            TotalPoints INTEGER NOT NULL
        );
        """);

        UpgradeMirrorTables(db);

        return sp;
    }

    /// <summary>
    /// Brings player/team/club tables created by older builds up to the current mirror schema in place.
    /// Old columns (SyncedAPI, SyncedWS, AvatarName) are left alone - SQLite keeps them harmlessly.
    /// </summary>
    private static void UpgradeMirrorTables(DataContext db)
    {
        AddColumnIfMissing(db, "player", "RemoteId", "INTEGER NULL");
        AddColumnIfMissing(db, "player", "ShortcutNumber", "INTEGER NULL");
        AddColumnIfMissing(db, "player", "PhotoPath", "TEXT NULL");
        AddColumnIfMissing(db, "player", "AvatarId", "INTEGER NULL");
        AddColumnIfMissing(db, "player", "TeamId", "INTEGER NULL");
        AddColumnIfMissing(db, "player", "Level", "INTEGER NULL");
        AddColumnIfMissing(db, "player", "Country", "TEXT NULL");
        AddColumnIfMissing(db, "player", "City", "TEXT NULL");
        AddColumnIfMissing(db, "player", "LicenseNo", "TEXT NULL");
        AddColumnIfMissing(db, "player", "LicenseValidUntil", "TEXT NULL");
        AddColumnIfMissing(db, "player", "AssociationName", "TEXT NULL");
        AddColumnIfMissing(db, "player", "UpdatedAt", "TEXT NULL");
        AddColumnIfMissing(db, "player", "IsSystem", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(db, "player", "SystemSlot", "INTEGER NULL");

        AddColumnIfMissing(db, "team", "RemoteId", "INTEGER NULL");
        AddColumnIfMissing(db, "team", "ClubId", "INTEGER NULL");
        AddColumnIfMissing(db, "team", "UpdatedAt", "TEXT NULL");

        // Older kiosks let the placeholder players answer to shortcut numbers 1 and 2, which would now collide
        // with real players' shortcuts.
        db.Database.ExecuteSqlRaw("UPDATE player SET ShortcutNumber = NULL WHERE Id IN (1, 2)");

        db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_player_RemoteId ON player (RemoteId) WHERE RemoteId IS NOT NULL");
        db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_team_RemoteId ON team (RemoteId) WHERE RemoteId IS NOT NULL");
        db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_club_RemoteId ON club (RemoteId) WHERE RemoteId IS NOT NULL");
    }

    private static void AddColumnIfMissing(DataContext db, string table, string column, string definition)
    {
        var connection = db.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = '{column}'";
            if (Convert.ToInt32(command.ExecuteScalar()) == 0)
            {
                using var alter = connection.CreateCommand();
                alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
                alter.ExecuteNonQuery();
            }
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }
}
