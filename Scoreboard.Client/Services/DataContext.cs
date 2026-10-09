using Microsoft.EntityFrameworkCore;
using Scoreboard.Client.Models;

namespace Scoreboard.Client.Services;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<ScoreboardState> ScoreboardStateSet { get; set; }
    public DbSet<Player> PlayerSet { get; set; }
    public DbSet<MatchResult> MatchResultSet { get; set; }
    public DbSet<Team> TeamSet { get; set; }
    public DbSet<Club> ClubSet { get; set; }
    public DbSet<MatchHistory> MatchHistorySet { get; set; }
    public DbSet<Setting> SettingsSet { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScoreboardState>().HasKey(p => p.Id);
        modelBuilder.Entity<ScoreboardState>().Property(e => e.Id).UseAutoincrement();
        modelBuilder.Entity<ScoreboardState>().ToTable("scoreboard_state");

        modelBuilder.Entity<Player>().HasKey(p => p.Id);
        modelBuilder.Entity<Player>().Property(p => p.Id).ValueGeneratedNever();
        modelBuilder.Entity<Player>().ToTable("player");

        modelBuilder.Entity<Team>().HasKey(t => t.Id);
        modelBuilder.Entity<Team>().Property(t => t.Id).ValueGeneratedNever();
        modelBuilder.Entity<Team>().ToTable("team");

        modelBuilder.Entity<Club>().HasKey(c => c.Id);
        modelBuilder.Entity<Club>().Property(c => c.Id).ValueGeneratedNever();
        modelBuilder.Entity<Club>().ToTable("club");

        modelBuilder.Entity<MatchResult>().HasKey(m => m.Id);
        modelBuilder.Entity<MatchResult>().Property(m => m.Id).UseAutoincrement();
        modelBuilder.Entity<MatchResult>().ToTable("match_result");

        modelBuilder.Entity<MatchHistory>().HasKey(h => h.Id);
        modelBuilder.Entity<MatchHistory>().Property(h => h.Id).UseAutoincrement();
        modelBuilder.Entity<MatchHistory>().ToTable("match_history");

        modelBuilder.Entity<Setting>().HasKey(s => s.Id);
        modelBuilder.Entity<Setting>().ToTable("Settings");
    }
}
