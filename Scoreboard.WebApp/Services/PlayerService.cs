using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Models;

namespace Scoreboard.WebApp.Services;

public class PlayerService(ScoreboardDbContext db, IWebHostEnvironment env)
{
    public Task<List<Player>> ListForClubAsync(int clubId) =>
        db.PlayerSet.Where(p => p.ClubId == clubId).OrderBy(p => p.Name).ToListAsync();

    public Task<Player?> GetAsync(int clubId, int id) =>
        db.PlayerSet.FirstOrDefaultAsync(p => p.Id == id && p.ClubId == clubId);

    public async Task<Player> UpsertAsync(
        int clubId,
        int id,
        string nickname,
        string name,
        int? avatarId,
        string email,
        Level level,
        string baseCountry,
        string baseCity,
        string? photoBase64,
        string? photoExtension)
    {
        var player = id != 0 ? await db.PlayerSet.FirstOrDefaultAsync(p => p.Id == id && p.ClubId == clubId) : null;
        if (player is null)
        {
            player = new Player { ClubId = clubId };
            db.PlayerSet.Add(player);
        }

        player.Nickname = nickname;
        player.Name = name;
        player.AvatarId = avatarId;
        player.Email = email;
        player.Level = level;
        player.BaseCountry = baseCountry;
        player.BaseCity = baseCity;
        player.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(photoBase64))
        {
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(photoBase64);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid photo data.");
            }

            var extension = string.IsNullOrWhiteSpace(photoExtension) ? "jpg" : photoExtension.TrimStart('.');
            var folder = Path.Combine(env.WebRootPath, "Players", clubId.ToString());
            Directory.CreateDirectory(folder);
            var fileName = $"{player.Id}.{extension}";
            await File.WriteAllBytesAsync(Path.Combine(folder, fileName), bytes);
            player.PhotoPath = $"Players/{clubId}/{fileName}";

            await db.SaveChangesAsync();
        }

        return player;
    }

    public async Task<bool> DeleteAsync(int clubId, int id)
    {
        var player = await db.PlayerSet.FirstOrDefaultAsync(p => p.Id == id && p.ClubId == clubId);
        if (player is null)
        {
            return false;
        }

        db.PlayerSet.Remove(player);
        await db.SaveChangesAsync();
        return true;
    }
}
