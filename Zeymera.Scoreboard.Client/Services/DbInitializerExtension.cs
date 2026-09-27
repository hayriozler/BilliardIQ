using Microsoft.EntityFrameworkCore;

namespace Zeymera.Scoreboard.Client.Services;

public static class DbInitializerExtension
{
    public static IServiceProvider InitializeDbAsync(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        db.Database.EnsureCreated();

        db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS scoreboard_state (
            Id INTEGER PRIMARY KEY,
            Player1Name TEXT NOT NULL DEFAULT '',
            Player2Name TEXT NOT NULL DEFAULT '',
            Player1Id INTEGER NULL,
            Player2Id INTEGER NULL,
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
         INSERT INTO player (Id, Nickname, Name, SyncedAPI, SyncedWS)
         VALUES (1, 'Player1Label', '', 0, 0),
                (2, 'Player2Label', '', 0, 0)
                ON CONFLICT(Id) DO NOTHING;
        """);

        db.Database.ExecuteSqlRaw("""
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
