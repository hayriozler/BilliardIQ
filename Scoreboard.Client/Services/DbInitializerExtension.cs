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
            Nickname TEXT NOT NULL,
            Name TEXT NOT NULL,
            RemoteId INTEGER NULL,
            PhotoPath TEXT NULL,
            AvatarId INTEGER NULL,
            AvatarName TEXT NULL,
            TeamId INTEGER NULL,
            ShortcutNumber INTEGER NULL,
            SyncedAPI INTEGER NOT NULL DEFAULT 0,
            SyncedWS INTEGER NOT NULL DEFAULT 0
        );
        """);

        db.Database.ExecuteSqlRaw("""         
         INSERT INTO player (Id, Nickname, Name, ShortcutNumber, SyncedAPI, SyncedWS)
         VALUES (1, 'P1', 'Player 1', 1, 0, 0),
                (2, 'P2', 'Player 2', 2, 0, 0)
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
            SyncedAPI INTEGER NOT NULL DEFAULT 0,
            SyncedWS INTEGER NOT NULL DEFAULT 0
        );
        """);

        db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS team (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL,
            RemoteId INTEGER NULL,
            SyncedAPI INTEGER NOT NULL DEFAULT 0,
            SyncedWS INTEGER NOT NULL DEFAULT 0
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
}
