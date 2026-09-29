namespace Scoreboard.WebApp.Requests;

/// <param name="ClubId">Optional; teams created without one join the salon's first club.</param>
public record UpsertTeamRequest(int Id, string Name, int? ClubId = null);
