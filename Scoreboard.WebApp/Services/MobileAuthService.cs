using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Security;

namespace Scoreboard.WebApp.Services;

public record MobileOrganization(int Id, string Name);

public record MobileUser(int Id, string DisplayName, string? Email, string Locale, int? PlayerId);

public record MobileSession(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string? RefreshToken,
    string Role,
    bool MustChangePassword,
    bool NeedsOrganization,
    MobileUser User,
    MobileOrganization? Organization,
    IReadOnlyList<MobileOrganization> Organizations);

public record PlayerAccount(int PlayerId, string Email, string TemporaryPassword);

public record PlayerInviteInfo(string Code, DateTimeOffset ExpiresAt);

public class MobileAuthService(DataContext db, AuthService auth, JwtTokenService tokens, JwtSettings settings)
{
    private static readonly PasswordHasher<User> _hasher = new();
    private const string PasswordAlphabet = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private static readonly TimeSpan _inviteLifetime = TimeSpan.FromDays(14);

    private sealed record Identity(string Role, IReadOnlyList<MobileOrganization> Organizations, MobileOrganization? Organization, StaffMember? Staff, Player? Player);

    public async Task<MobileSession?> LoginAsync(string email, string password, string? deviceName)
    {
        var user = await auth.VerifyAsync(email, password);
        return user is null ? null : await StartSessionAsync(user, null, deviceName);
    }

    public async Task<MobileSession?> RefreshAsync(string refreshToken)
    {
        var hash = Hash(refreshToken);
        var stored = await db.RefreshTokenSet.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (stored is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (stored.RevokedAt is not null)
        {
            await RevokeAllAsync(stored.UserId, null);
            return null;
        }

        if (stored.ExpiresAt <= now || stored.User.Status != UserStatus.Active)
        {
            return null;
        }

        var identity = await ResolveAsync(stored.User, stored.OrganizationId);
        if (identity is null)
        {
            return null;
        }

        stored.RevokedAt = now;
        var next = NewRefreshToken(stored.UserId, identity.Organization?.Id, stored.DeviceName, out var raw);
        db.RefreshTokenSet.Add(next);
        await db.SaveChangesAsync();
        return BuildSession(stored.User, identity, next, raw);
    }

    public async Task<MobileSession?> SelectOrganizationAsync(int userId, int refreshTokenId, int organizationId)
    {
        var stored = await db.RefreshTokenSet.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == refreshTokenId && t.UserId == userId && t.RevokedAt == null);
        if (stored is null)
        {
            return null;
        }

        var identity = await ResolveAsync(stored.User, organizationId);
        if (identity?.Organization is null)
        {
            return null;
        }

        stored.OrganizationId = organizationId;
        await db.SaveChangesAsync();
        return BuildSession(stored.User, identity, stored, null);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var hash = Hash(refreshToken);
        var stored = await db.RefreshTokenSet.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task<MobileSession> ChangePasswordAsync(int userId, int refreshTokenId, string currentPassword, string newPassword)
    {
        await auth.ChangePasswordAsync(userId, currentPassword, newPassword);
        var user = await db.UserSet.FirstAsync(u => u.Id == userId);
        user.MustChangePassword = false;
        await db.SaveChangesAsync();
        await RevokeAllAsync(userId, refreshTokenId);

        var stored = await db.RefreshTokenSet.FirstAsync(t => t.Id == refreshTokenId);
        var identity = await ResolveAsync(user, stored.OrganizationId) ?? throw new ArgumentException("Kullanıcı bulunamadı.");
        return BuildSession(user, identity, stored, null);
    }

    public async Task<MobileSession?> GetSessionAsync(int userId, int refreshTokenId)
    {
        var stored = await db.RefreshTokenSet.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == refreshTokenId && t.UserId == userId && t.RevokedAt == null);
        if (stored is null)
        {
            return null;
        }

        var identity = await ResolveAsync(stored.User, stored.OrganizationId);
        return identity is null ? null : BuildSession(stored.User, identity, stored, null);
    }

    public async Task UpdateEmailAsync(int userId, string email, string password)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        if (!email.Contains('@'))
        {
            throw new ArgumentException("Geçerli bir e-posta girin.");
        }

        var user = await db.UserSet.FirstAsync(u => u.Id == userId);
        if (await auth.VerifyAsync(user.Email ?? "", password) is null)
        {
            throw new ArgumentException("Mevcut şifre yanlış.");
        }

        if (await db.UserSet.AnyAsync(u => u.Email == email && u.Id != userId))
        {
            throw new ArgumentException("Bu e-posta zaten kullanılıyor.");
        }

        user.Email = email;
        await db.SaveUniqueAsync("Email", "Bu e-posta zaten kullanılıyor.");
    }

    public async Task UpdateProfileAsync(int userId, string? displayName, string? locale, string? phone)
    {
        var user = await db.UserSet.FirstAsync(u => u.Id == userId);
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            user.DisplayName = displayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(locale))
        {
            user.Locale = locale.Trim();
        }

        user.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        await db.SaveUniqueAsync("Phone", "Bu telefon numarası zaten kullanılıyor.");
    }

    public async Task<PlayerAccount> CreatePlayerAccountAsync(int playerId, string email)
    {
        var player = await db.PlayerSet.FirstOrDefaultAsync(p => p.Id == playerId && p.DeletedAt == null && !p.IsSystem)
            ?? throw new ArgumentException("Oyuncu bulunamadı.");
        if (player.UserId is not null)
        {
            throw new ArgumentException("Bu oyuncunun zaten bir hesabı var.");
        }

        email = (email ?? "").Trim().ToLowerInvariant();
        if (!email.Contains('@'))
        {
            throw new ArgumentException("Geçerli bir e-posta girin.");
        }

        if (await db.UserSet.AnyAsync(u => u.Email == email))
        {
            throw new ArgumentException("Bu e-posta zaten kullanılıyor.");
        }

        var password = RandomText(PasswordAlphabet, 8);
        var user = new User
        {
            Email = email,
            DisplayName = player.DisplayName,
            Status = UserStatus.Active,
            OrganizationId = db.CurrentOrganizationId,
            MustChangePassword = true
        };
        user.PasswordHash = _hasher.HashPassword(user, password);
        db.UserSet.Add(user);
        player.User = user;
        player.Email ??= email;
        await db.SaveUniqueAsync("Email", "Bu e-posta zaten kullanılıyor.");
        return new PlayerAccount(player.Id, email, password);
    }

    public async Task<PlayerAccount> ResetPlayerPasswordAsync(int playerId)
    {
        var player = await db.PlayerSet.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == playerId && p.DeletedAt == null)
            ?? throw new ArgumentException("Oyuncu bulunamadı.");
        var user = player.User ?? throw new ArgumentException("Bu oyuncunun hesabı yok.");

        var password = RandomText(PasswordAlphabet, 8);
        user.PasswordHash = _hasher.HashPassword(user, password);
        user.MustChangePassword = true;
        await db.SaveChangesAsync();
        await RevokeAllAsync(user.Id, null);
        return new PlayerAccount(player.Id, user.Email ?? "", password);
    }

    public async Task<PlayerInviteInfo> CreateInviteAsync(int playerId)
    {
        var player = await db.PlayerSet.FirstOrDefaultAsync(p => p.Id == playerId && p.DeletedAt == null && !p.IsSystem)
            ?? throw new ArgumentException("Oyuncu bulunamadı.");
        if (player.UserId is not null)
        {
            throw new ArgumentException("Bu oyuncunun zaten bir hesabı var.");
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var old in await db.PlayerInviteSet.Where(i => i.PlayerId == playerId && i.UsedAt == null).ToListAsync())
        {
            old.UsedAt = now;
        }

        string code;
        do
        {
            code = RandomText(CodeAlphabet, 8);
        }
        while (await db.PlayerInviteSet.IgnoreQueryFilters().AnyAsync(i => i.Code == code));

        var invite = new PlayerInvite { OrganizationId = db.CurrentOrganizationId, PlayerId = playerId, Code = code, ExpiresAt = now + _inviteLifetime };
        db.PlayerInviteSet.Add(invite);
        await db.SaveChangesAsync();
        return new PlayerInviteInfo(code, invite.ExpiresAt);
    }

    public async Task<MobileSession> RegisterWithInviteAsync(string code, string email, string password, string? deviceName)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        email = (email ?? "").Trim().ToLowerInvariant();
        if (!email.Contains('@'))
        {
            throw new ArgumentException("Geçerli bir e-posta girin.");
        }

        if (string.IsNullOrEmpty(password) || password.Length < AuthService.MinPasswordLength)
        {
            throw new LocalizedArgumentException("Şifre en az {0} karakter olmalı.", AuthService.MinPasswordLength);
        }

        var now = DateTimeOffset.UtcNow;
        var invite = await db.PlayerInviteSet.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Code == code && i.UsedAt == null && i.ExpiresAt > now)
            ?? throw new ArgumentException("Davet kodu geçersiz veya süresi dolmuş.");
        var player = await db.PlayerSet.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == invite.PlayerId && p.DeletedAt == null)
            ?? throw new ArgumentException("Oyuncu bulunamadı.");
        if (player.UserId is not null)
        {
            throw new ArgumentException("Bu oyuncunun zaten bir hesabı var.");
        }

        if (await db.UserSet.AnyAsync(u => u.Email == email))
        {
            throw new ArgumentException("Bu e-posta zaten kullanılıyor.");
        }

        var user = new User
        {
            Email = email,
            DisplayName = player.DisplayName,
            Status = UserStatus.Active,
            OrganizationId = invite.OrganizationId
        };
        user.PasswordHash = _hasher.HashPassword(user, password);
        db.UserSet.Add(user);
        player.User = user;
        player.Email ??= email;
        invite.UsedAt = now;
        await db.SaveUniqueAsync("Email", "Bu e-posta zaten kullanılıyor.");
        return (await StartSessionAsync(user, null, deviceName))!;
    }

    private async Task<MobileSession?> StartSessionAsync(User user, int? organizationId, string? deviceName)
    {
        var identity = await ResolveAsync(user, organizationId);
        if (identity is null)
        {
            return null;
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        var token = NewRefreshToken(user.Id, identity.Organization?.Id, deviceName, out var raw);
        db.RefreshTokenSet.Add(token);
        await db.SaveChangesAsync();
        return BuildSession(user, identity, token, raw);
    }

    private async Task<Identity?> ResolveAsync(User user, int? organizationId)
    {
        if (user.Status != UserStatus.Active)
        {
            return null;
        }

        var memberships = await auth.MembershipsAsync(user.Id);
        if (memberships.Count > 0)
        {
            var organizations = memberships.Select(m => new MobileOrganization(m.OrganizationId, m.Organization.Name)).ToList();
            var chosen = organizationId is { } id
                ? memberships.FirstOrDefault(m => m.OrganizationId == id)
                : memberships.Count == 1 ? memberships[0] : null;
            if (organizationId is not null && chosen is null)
            {
                return null;
            }

            var isAdmin = user.OrganizationId is null;
            var role = isAdmin ? MobileClaims.Admin
                : chosen is null || chosen.Roles.Contains(StaffRole.Owner) || chosen.Roles.Contains(StaffRole.Manager) ? MobileClaims.Manager
                : MobileClaims.Staff;
            return new Identity(role, organizations, chosen is null ? null : new MobileOrganization(chosen.OrganizationId, chosen.Organization.Name), chosen, null);
        }

        var player = await db.PlayerSet.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.UserId == user.Id && p.DeletedAt == null);
        if (player?.CreatedInOrganizationId is not int playerOrganizationId)
        {
            return null;
        }

        var organization = await db.OrganizationSet.Where(o => o.Id == playerOrganizationId && o.IsActive && o.DeletedAt == null)
            .Select(o => new MobileOrganization(o.Id, o.Name)).FirstOrDefaultAsync();
        return organization is null ? null : new Identity(MobileClaims.Player, [organization], organization, null, player);
    }

    private MobileSession BuildSession(User user, Identity identity, RefreshToken token, string? rawRefreshToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, identity.Role),
            new(MobileClaims.RefreshTokenId, token.Id.ToString())
        };
        if (identity.Organization is { } organization)
        {
            claims.Add(new Claim(AuthClaims.OrganizationId, organization.Id.ToString()));
            claims.Add(new Claim(AuthClaims.OrganizationName, organization.Name));
        }

        if (identity.Staff is { } staff)
        {
            claims.Add(new Claim(AuthClaims.StaffMemberId, staff.Id.ToString()));
            claims.AddRange(staff.Roles.Select(r => new Claim(ClaimTypes.Role, r.ToString())));
        }

        if (identity.Player is { } player)
        {
            claims.Add(new Claim(MobileClaims.PlayerId, player.Id.ToString()));
        }

        if (user.MustChangePassword)
        {
            claims.Add(new Claim(MobileClaims.MustChangePassword, "1"));
        }

        var access = tokens.Create(claims, out var expiresAt);
        return new MobileSession(
            access, expiresAt, rawRefreshToken, identity.Role, user.MustChangePassword,
            identity.Organization is null,
            new MobileUser(user.Id, user.DisplayName, user.Email, user.Locale, identity.Player?.Id),
            identity.Organization,
            identity.Organizations);
    }

    private RefreshToken NewRefreshToken(int userId, int? organizationId, string? deviceName, out string raw)
    {
        raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var now = DateTimeOffset.UtcNow;
        return new RefreshToken
        {
            UserId = userId,
            OrganizationId = organizationId,
            TokenHash = Hash(raw),
            DeviceName = deviceName is { Length: > 100 } ? deviceName[..100] : deviceName,
            ExpiresAt = now.AddDays(settings.RefreshDays),
            LastUsedAt = now
        };
    }

    private async Task RevokeAllAsync(int userId, int? exceptTokenId)
    {
        var now = DateTimeOffset.UtcNow;
        var open = await db.RefreshTokenSet.Where(t => t.UserId == userId && t.RevokedAt == null && t.Id != exceptTokenId).ToListAsync();
        foreach (var token in open)
        {
            token.RevokedAt = now;
        }

        await db.SaveChangesAsync();
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string RandomText(string alphabet, int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return new string(chars);
    }
}
