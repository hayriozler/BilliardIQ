using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Models;

namespace Scoreboard.WebApp.Data;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<Club> ClubSet => Set<Club>();
    public DbSet<ScoreboardClient> ScoreboardClientSet => Set<ScoreboardClient>();
    public DbSet<Player> PlayerSet => Set<Player>();
    public DbSet<MatchStat> MatchStatSet => Set<MatchStat>();
    public DbSet<Team> TeamSet => Set<Team>();
    public DbSet<TeamPlayer> TeamPlayerSet => Set<TeamPlayer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Club>(entity =>
        {
            entity.Property(c => c.Code).IsRequired().HasMaxLength(10);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(c => c.Code).IsUnique();
        });

        modelBuilder.Entity<ScoreboardClient>(entity =>
        {
            entity.Property(c => c.Id).HasMaxLength(10);
            entity.Property(c => c.Name).HasMaxLength(200);

            entity.HasOne(c => c.Club)
                .WithMany()
                .HasForeignKey(c => c.ClubId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.Property(p => p.Nickname).HasMaxLength(100);
            entity.Property(p => p.Name).HasMaxLength(200);
            entity.Property(p => p.Email).HasMaxLength(200);
            entity.Property(p => p.BaseCountry).HasMaxLength(100);
            entity.Property(p => p.BaseCity).HasMaxLength(100);

            entity.HasOne(p => p.Club)
                .WithMany(c => c.Players)
                .HasForeignKey(p => p.ClubId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MatchStat>(entity =>
        {
            entity.Property(s => s.ClientId).HasMaxLength(10);
            entity.Property(s => s.Player1Name).HasMaxLength(100);
            entity.Property(s => s.Player2Name).HasMaxLength(100);

            entity.HasOne<ScoreboardClient>()
                .WithMany()
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(200);

            entity.HasOne(t => t.Club)
                .WithMany(c => c.Teams)
                .HasForeignKey(t => t.ClubId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeamPlayer>(entity =>
        {
            entity.HasKey(tp => new { tp.TeamId, tp.PlayerId });

            entity.HasOne(tp => tp.Team)
                .WithMany(t => t.TeamPlayers)
                .HasForeignKey(tp => tp.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tp => tp.Player)
                .WithMany()
                .HasForeignKey(tp => tp.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
