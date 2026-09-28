namespace Zeymera.BillardIQ.WebApp.Responses;

public record TeamDto(int Id, int ClubId, string Name, DateTimeOffset UpdatedAt, List<TeamPlayerDto> Players);

public record TeamPlayerDto(int Id, string Nickname, string Name);
