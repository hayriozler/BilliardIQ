using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Scoreboard.Client.Services;

public static class DbInitializerExtension
{
    public static IServiceProvider InitializeDbAsync(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        db.Database.EnsureCreated();
        ConvertToServerIds(db);
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
            Nickname TEXT NOT NULL,
            Name TEXT NOT NULL,
            ShortcutNumber INTEGER NULL,
            PhotoPath TEXT NULL,
            AvatarId INTEGER NULL,
            TeamId INTEGER NULL,
            UpdatedAt TEXT NULL,
            IsSystem INTEGER NOT NULL DEFAULT 0,
            SystemSlot INTEGER NULL
        );
        """);

        // Players 1 and 2 are what the board shows when nobody is picked. With a remote server configured they are
        // the server's system players (with the server's ids) and arrive with the first pull; without one there is no
        // server id, so they are created right here with Id 1 and 2.
        if (!scope.ServiceProvider.GetRequiredService<IOptions<RemoteSyncOptions>>().Value.IsConfigured)
        {
            db.Database.ExecuteSqlRaw("""
             INSERT INTO player (Id, Nickname, Name, IsSystem, SystemSlot)
             VALUES (1, 'P1', 'Player 1', 1, 1),
                    (2, 'P2', 'Player 2', 1, 2)
                    ON CONFLICT(Id) DO NOTHING;
            """);
        }

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
            Name TEXT NOT NULL,
            ShortName TEXT NOT NULL DEFAULT '',
            City TEXT NULL,
            PrimaryColor TEXT NULL
        );
        """);

        db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS team (
            Id INTEGER PRIMARY KEY,
            ClubId INTEGER NULL,
            AvatarId INTEGER NULL,
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


        return sp;
    }

    private static int Count(DataContext db, string sql) =>
        db.Database.SqlQueryRaw<int>(sql).AsEnumerable().First();

    /// <summary>
    /// Kiosks set up by older builds keyed the player/team/club mirror by a local generated id plus a RemoteId column.
    /// The mirror now uses the server's id as the key. Those tables are only a copy of the server, so they are dropped
    /// and refilled by the next pull; what must survive keeps pointing at the same people: unsent match results and the
    /// board's selection get the server id of their player (0 = unknown, for the local Player 1 / Player 2 seeds).
    /// </summary>
    private static void ConvertToServerIds(DataContext db)
    {
        if (Count(db, "SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = 'player'") == 0)
        {
            return;
        }

        var hasRemoteId = Count(db, "SELECT COUNT(*) AS Value FROM pragma_table_info('player') WHERE name = 'RemoteId'") > 0;
        var hasSystemFlag = Count(db, "SELECT COUNT(*) AS Value FROM pragma_table_info('player') WHERE name = 'IsSystem'") > 0;
        if (!hasRemoteId && hasSystemFlag)
        {
            return;
        }

        if (hasRemoteId)
        {
            foreach (var (table, column) in new[]
                     {
                         ("match_result", "Player1Id"), ("match_result", "Player2Id"),
                         ("scoreboard_state", "Player1Id"), ("scoreboard_state", "Player2Id")
                     })
            {
#pragma warning disable EF1002 // table / column names come from the literal list above, not from input
                db.Database.ExecuteSqlRaw(
                    $"UPDATE {table} SET {column} = COALESCE((SELECT RemoteId FROM player WHERE player.Id = {table}.{column}), 0)");
#pragma warning restore EF1002
            }
        }

        db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS player");
        db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS team");
        db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS club");
    }
}
