namespace BilliardIQ.Mobile.Services.Api;

public static class ApiRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
    public const string Player = "Player";
}

public sealed record ApiOrganization(int Id, string Name);

public sealed record ApiUser(int Id, string DisplayName, string? Email, string Locale, int? PlayerId);

public sealed record ApiSession(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string? RefreshToken,
    string Role,
    bool MustChangePassword,
    bool NeedsOrganization,
    ApiUser User,
    ApiOrganization? Organization,
    List<ApiOrganization> Organizations);

public sealed record LoginRequest(string Email, string Password, string? DeviceName);

public sealed record RefreshRequest(string RefreshToken);

public sealed record SelectOrganizationRequest(int OrganizationId);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record InviteRegisterRequest(string Code, string Email, string Password, string? DeviceName);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword);

public sealed record ChangeEmailRequest(string Email, string Password);

public sealed record UpdateProfileRequest(string? DisplayName, string? Locale, string? Phone);

public sealed record PlayerProfileDto(int Id, string Name, string? Nickname, string DisplayName, int? AvatarId, string? PhotoUrl, int Level, int? ShortcutNumber);

public sealed record UpdatePlayerProfileRequest(string Name, string? Nickname, int? AvatarId, string? PhotoBase64, string? PhotoExtension);

public sealed record StatSummaryDto(int Matches, int Wins, int Losses, double WinPercent, double AverageInnings, double AveragePerInning, double BestAverage, int BestHighRun);

public sealed record PlayerMatchDto(int MatchId, DateTimeOffset PlayedAt, string OpponentName, int PlayerScore, int OpponentScore, int Inning, int HighRun, double Average, bool Won);

public sealed record PlayerHomeDto(PlayerProfileDto Player, StatSummaryDto Summary, List<PlayerMatchDto> LastMatches);

public sealed record ManageTableDto(int Id, int Number, int? ScoreboardNo, string? Label, int Type, int Status, DateTimeOffset? SessionOpenedAt);

public sealed record ManagePlayerDto(int Id, string Nickname, string Name, string? PhotoPath, int? AvatarId, int Level, int? ShortcutNumber, string? AssociationName, bool IsSystem);

public sealed record ManageTeamPlayerDto(int Id, string Nickname, string Name);

public sealed record ManageTeamDto(int Id, string Name, List<ManageTeamPlayerDto> Players, int? AvatarId);



public sealed class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
