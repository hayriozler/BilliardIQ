namespace Scoreboard.WebApp.Requests;

public record UpsertTeamRequest(int Id, string Name, int? ClubId = null, int? AvatarId = null);
