namespace Scoreboard.WebApp.Requests;

public record UpsertClubRequest(int Id, string Name, string? ShortName, string? City, string? PrimaryColor);
