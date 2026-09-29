using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class PlayerService(DataContext db, IWebHostEnvironment env)
{
    public Task<List<Player>> ListForOrganizationAsync(int organizationId) =>
        db.PlayerSet
            .Where(p => p.CreatedInOrganizationId == organizationId && p.DeletedAt == null)
            .OrderBy(p => p.DisplayName)
            .ToListAsync();

    public Task<Player?> GetAsync(int organizationId, int id) =>
        db.PlayerSet.FirstOrDefaultAsync(p =>
            p.Id == id && p.CreatedInOrganizationId == organizationId && p.DeletedAt == null);

    public async Task<Player> UpsertAsync(
        int organizationId,
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
        name = name.Trim();
        if (name.Length == 0 && string.IsNullOrWhiteSpace(nickname))
        {
            throw new ArgumentException("Ad veya takma ad gerekli.");
        }

        var player = id != 0 ? await GetAsync(organizationId, id) : null;
        if (player is null)
        {
            player = new Player { CreatedInOrganizationId = organizationId };
            db.PlayerSet.Add(player);
        }

        var (first, last) = SplitName(name);
        player.FirstName = first;
        player.LastName = last;
        player.Nickname = string.IsNullOrWhiteSpace(nickname) ? null : nickname.Trim();
        player.DisplayName = player.Nickname ?? name;
        player.AvatarId = avatarId;
        player.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        player.Level = level;
        player.Nationality = string.IsNullOrWhiteSpace(baseCountry) ? null : baseCountry.Trim();
        player.City = string.IsNullOrWhiteSpace(baseCity) ? null : baseCity.Trim();

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
                throw new ArgumentException("Geçersiz fotoğraf verisi.");
            }

            var extension = string.IsNullOrWhiteSpace(photoExtension) ? "jpg" : photoExtension.TrimStart('.');
            var folder = Path.Combine(env.WebRootPath, "Players", organizationId.ToString());
            Directory.CreateDirectory(folder);
            var fileName = $"{player.Id}.{extension}";
            await File.WriteAllBytesAsync(Path.Combine(folder, fileName), bytes);
            player.PhotoUrl = $"Players/{organizationId}/{fileName}";

            await db.SaveChangesAsync();
        }

        return player;
    }

    public async Task<bool> DeleteAsync(int organizationId, int id)
    {
        var player = await GetAsync(organizationId, id);
        if (player is null)
        {
            return false;
        }

        // Match history keeps referencing the player, so the profile is retired rather than removed.
        db.TeamMemberSet.RemoveRange(await db.TeamMemberSet.Where(m => m.PlayerId == id).ToListAsync());
        player.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    private static (string First, string Last) SplitName(string name)
    {
        var index = name.LastIndexOf(' ');
        return index < 0 ? (name, "") : (name[..index].Trim(), name[(index + 1)..].Trim());
    }
}
