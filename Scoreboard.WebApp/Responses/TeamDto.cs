namespace Scoreboard.WebApp.Responses;

public record TeamDto(int Id, int ClubId, string Name, DateTimeOffset UpdatedAt, List<TeamPlayerDto> Players, int? AvatarId = null);

public record TeamPlayerDto(int Id, string Nickname, string Name);
