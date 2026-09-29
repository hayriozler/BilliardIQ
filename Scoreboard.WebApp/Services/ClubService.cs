using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Models;

namespace Scoreboard.WebApp.Services;

public class ClubService(DataContext db)
{
    public Task<List<Club>> ListAsync() =>
        db.ClubSet.OrderBy(c => c.Name).ToListAsync();

    public Task<Club?> GetAsync(int id) =>
        db.ClubSet.FirstOrDefaultAsync(c => c.Id == id);

    public Task<Club?> FindByCodeAsync(string code) =>
        db.ClubSet.FirstOrDefaultAsync(c => c.Code == code);

    public async Task<Club> CreateAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Club name is required.");
        }

        string code;
        do
        {
            code = PairingCodeGenerator.Generate();
        } while (await db.ClubSet.AnyAsync(c => c.Code == code));

        var club = new Club { Name = name.Trim(), Code = code };
        db.ClubSet.Add(club);
        await db.SaveChangesAsync();
        return club;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var club = await db.ClubSet.FindAsync(id);
        if (club is null)
        {
            return false;
        }

        db.ClubSet.Remove(club);
        await db.SaveChangesAsync();
        return true;
    }
}
