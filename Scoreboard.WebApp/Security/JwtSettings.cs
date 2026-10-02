using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Scoreboard.WebApp.Security;

public class JwtSettings
{
    public const string Scheme = "Bearer";
    public const string MobilePolicy = "Mobile";
    public const string MobileAnyPolicy = "MobileAny";
    public const string MobileManagerPolicy = "MobileManager";
    public const string MobilePlayerPolicy = "MobilePlayer";

    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "BilliardIQ";
    public string Audience { get; set; } = "BilliardIQ.Mobile";
    public int AccessMinutes { get; set; } = 30;
    public int RefreshDays { get; set; } = 90;

    private SymmetricSecurityKey? _signingKey;

    public SymmetricSecurityKey SigningKey => _signingKey ??= new(Encoding.UTF8.GetBytes(Key));
}

public class JwtTokenService(JwtSettings settings)
{
    public string Create(IEnumerable<Claim> claims, out DateTimeOffset expiresAt)
    {
        expiresAt = DateTimeOffset.UtcNow.AddMinutes(settings.AccessMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}

public static class MobileClaims
{
    public const string PlayerId = "PlayerId";
    public const string RefreshTokenId = "RefreshTokenId";
    public const string MustChangePassword = "MustChangePassword";

    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
    public const string Player = "Player";

    public static bool HasOrganization(this ClaimsPrincipal user) => user.FindFirst(AuthClaims.OrganizationId) is not null;

    public static int GetPlayerId(this ClaimsPrincipal user) => int.Parse(user.FindFirst(PlayerId)!.Value);

    public static int GetRefreshTokenId(this ClaimsPrincipal user) => int.Parse(user.FindFirst(RefreshTokenId)!.Value);
}
