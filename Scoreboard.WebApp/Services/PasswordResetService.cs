using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public interface IPasswordResetNotifier
{
    Task SendAsync(string email, string displayName, string code, DateTimeOffset expiresAt);
}

public sealed class LoggingPasswordResetNotifier(ILogger<LoggingPasswordResetNotifier> logger) : IPasswordResetNotifier
{
    public Task SendAsync(string email, string displayName, string code, DateTimeOffset expiresAt)
    {
        logger.LogWarning("Password reset code for {Email}: {Code} (valid until {ExpiresAt:u}). No mail transport is configured.", email, code, expiresAt);
        return Task.CompletedTask;
    }
}

public class PasswordResetService(DataContext db, AuthService auth, IPasswordResetNotifier notifier)
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan _lifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan _resendDelay = TimeSpan.FromMinutes(1);

    public async Task RequestAsync(string email)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        var user = await db.UserSet.FirstOrDefaultAsync(u => u.Email == email);
        if (user?.PasswordHash is null || user.Status != UserStatus.Active)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var open = await db.PasswordResetCodeSet.Where(c => c.UserId == user.Id && c.UsedAt == null).ToListAsync();
        if (open.Any(c => now - c.CreatedAt < _resendDelay))
        {
            return;
        }

        foreach (var old in open)
        {
            old.UsedAt = now;
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var expiresAt = now + _lifetime;
        db.PasswordResetCodeSet.Add(new PasswordResetCode { UserId = user.Id, CodeHash = Hash(user.Id, code), ExpiresAt = expiresAt });
        await db.SaveChangesAsync();
        await notifier.SendAsync(user.Email!, user.DisplayName, code, expiresAt);
    }

    public async Task ResetAsync(string email, string code, string newPassword)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        var user = await db.UserSet.FirstOrDefaultAsync(u => u.Email == email);
        var now = DateTimeOffset.UtcNow;
        var record = user is null ? null : await db.PasswordResetCodeSet
            .Where(c => c.UserId == user.Id && c.UsedAt == null && c.ExpiresAt > now)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();
        if (user is null || record is null || record.Attempts >= MaxAttempts)
        {
            throw new ArgumentException("Kod geçersiz ya da süresi dolmuş.");
        }

        record.Attempts++;
        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(record.CodeHash), Encoding.UTF8.GetBytes(Hash(user.Id, (code ?? "").Trim())));
        if (!matches)
        {
            await db.SaveChangesAsync();
            throw new ArgumentException("Kod geçersiz ya da süresi dolmuş.");
        }

        record.UsedAt = now;
        await auth.SetPasswordAsync(user, newPassword);

        var open = await db.RefreshTokenSet.Where(t => t.UserId == user.Id && t.RevokedAt == null).ToListAsync();
        foreach (var token in open)
        {
            token.RevokedAt = now;
        }

        await db.SaveChangesAsync();
    }

    private static string Hash(int userId, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}:{code}")));
}
