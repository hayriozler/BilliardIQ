using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public record LoginResult(User User, StaffMember Staff, Organization Organization);

public class AuthService(DataContext db, ClientIdService clientIds, SystemPlayerService systemPlayers, GeoSeedService geoSeed)
{
    private static readonly PasswordHasher<User> _hasher = new();

    public const int MinPasswordLength = 6;

    public async Task<LoginResult?> LoginAsync(string email, string password)
    {
        email = NormalizeEmail(email);
        if (email.Length == 0 || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var user = await db.UserSet.FirstOrDefaultAsync(u => u.Email == email);
        if (user?.PasswordHash is null || user.Status != UserStatus.Active)
        {
            return null;
        }

        var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
        }

        var staff = await db.StaffMemberSet
            .Include(s => s.Organization)
            .Where(s => s.UserId == user.Id && s.IsActive && s.Organization.IsActive && s.DeletedAt == null)
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync();
        if (staff is null)
        {
            return null;
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return new LoginResult(user, staff, staff.Organization);
    }

    /// <summary>Creates a salon (tenant) with its owner account. Tables and pricing are set up after registration.</summary>
    public async Task<LoginResult> RegisterOrganizationAsync(
        string organizationName, string ownerName, string email, string password, string? countryCode = null, string? currency = null, string? language = null)
    {
        organizationName = organizationName.Trim();
        ownerName = ownerName.Trim();
        email = NormalizeEmail(email);

        if (organizationName.Length == 0) throw new ArgumentException("Salon adı gerekli.");
        if (ownerName.Length == 0) throw new ArgumentException("Ad soyad gerekli.");
        if (!email.Contains('@')) throw new ArgumentException("Geçerli bir e-posta girin.");
        ValidatePassword(password);
        var country = string.IsNullOrWhiteSpace(countryCode)
            ? CountryCatalog.Find("TR")!
            : CountryCatalog.Find(countryCode) ?? throw new ArgumentException("Desteklenmeyen ülke.");
        currency = string.IsNullOrWhiteSpace(currency) ? country.Currency : currency.Trim().ToUpperInvariant();
        if (!CountryCatalog.IsCurrency(currency)) throw new ArgumentException("Desteklenmeyen para birimi.");
        // Asked on the form; when nothing was chosen the country's language is used.
        language = string.IsNullOrWhiteSpace(language) ? country.Language : language.Trim().ToLowerInvariant();
        if (!Loc.IsSupported(language)) throw new ArgumentException("Desteklenmeyen dil.");

        if (await db.UserSet.AnyAsync(u => u.Email == email))
        {
            throw new ArgumentException("Bu e-posta ile zaten bir hesap var.");
        }

        var organization = new Organization
        {
            Name = organizationName,
            Slug = await UniqueSlugAsync(organizationName),
            Code = await UniqueCodeAsync(),
            ClientId = await clientIds.GenerateUniqueAsync(),
            Address = new Address { Line1 = "", City = "", CountryCode = country.Code },
            Email = email,
            Language = language,
            CountryCode = country.Code,
            Currency = currency,
            TimeZone = country.TimeZone,
            Plan = SubscriptionPlan.Free
        };

        var user = new User { Email = email, DisplayName = ownerName, Status = UserStatus.Active };
        user.PasswordHash = _hasher.HashPassword(user, password);

        var staff = new StaffMember { Organization = organization, User = user, Roles = [StaffRole.Owner] };

        db.StaffMemberSet.Add(staff);
        await db.SaveChangesAsync();
        await systemPlayers.EnsureAsync(organization);
        await geoSeed.EnsureAsync(organization);
        return new LoginResult(user, staff, organization);
    }

    public Task<List<StaffMember>> ListStaffAsync(int organizationId) =>
        db.StaffMemberSet
            .Include(s => s.User)
            .Where(s => s.OrganizationId == organizationId && s.DeletedAt == null)
            .OrderBy(s => s.Id)
            .ToListAsync();

    public async Task<StaffMember> AddStaffAsync(int organizationId, string displayName, string email, string password, StaffRole role)
    {
        email = NormalizeEmail(email);
        if (displayName.Trim().Length == 0) throw new ArgumentException("Ad soyad gerekli.");
        if (!email.Contains('@')) throw new ArgumentException("Geçerli bir e-posta girin.");
        ValidatePassword(password);

        if (await db.UserSet.AnyAsync(u => u.Email == email))
        {
            throw new ArgumentException("Bu e-posta zaten kullanılıyor.");
        }

        var user = new User { Email = email, DisplayName = displayName.Trim(), Status = UserStatus.Active };
        user.PasswordHash = _hasher.HashPassword(user, password);

        var staff = new StaffMember { OrganizationId = organizationId, User = user, Roles = [role] };
        db.StaffMemberSet.Add(staff);
        await db.SaveChangesAsync();
        return staff;
    }

    public async Task SetStaffActiveAsync(int organizationId, int staffId, bool active)
    {
        var staff = await db.StaffMemberSet.FirstOrDefaultAsync(s => s.Id == staffId && s.OrganizationId == organizationId)
            ?? throw new ArgumentException("Personel bulunamadı.");
        if (!active && staff.Roles.Contains(StaffRole.Owner) &&
            await db.StaffMemberSet.CountAsync(s => s.OrganizationId == organizationId && s.IsActive && s.Id != staffId) == 0)
        {
            throw new ArgumentException("Son aktif kullanıcı devre dışı bırakılamaz.");
        }

        staff.IsActive = active;
        await db.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        ValidatePassword(newPassword);
        var user = await db.UserSet.FindAsync(userId) ?? throw new ArgumentException("Kullanıcı bulunamadı.");
        if (user.PasswordHash is null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
        {
            throw new ArgumentException("Mevcut şifre yanlış.");
        }

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        await db.SaveChangesAsync();
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
        {
            throw new LocalizedArgumentException("Şifre en az {0} karakter olmalı.", MinPasswordLength);
        }
    }

    private static string NormalizeEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();

    private async Task<string> UniqueCodeAsync()
    {
        string code;
        do
        {
            code = PairingCodeGenerator.Generate();
        } while (await db.OrganizationSet.AnyAsync(o => o.Code == code));

        return code;
    }

    private async Task<string> UniqueSlugAsync(string name)
    {
        var baseSlug = Slugify(name);
        var slug = baseSlug;
        for (var i = 2; await db.OrganizationSet.AnyAsync(o => o.Slug == slug); i++)
        {
            slug = $"{baseSlug}-{i}";
        }

        return slug;
    }

    private static string Slugify(string name)
    {
        var builder = new StringBuilder();
        foreach (var raw in name.Trim().ToLowerInvariant())
        {
            var c = raw switch
            {
                'ç' => 'c',
                'ğ' => 'g',
                'ı' => 'i',
                'ö' => 'o',
                'ş' => 's',
                'ü' => 'u',
                _ => raw
            };

            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(c);
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length == 0 ? "salon" : slug;
    }
}
