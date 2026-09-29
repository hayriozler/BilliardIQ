using Scoreboard.WebApp.Models;

namespace Scoreboard.WebApp.Requests;

public record UpsertPlayerRequest(
    int Id,
    string Nickname,
    string Name,
    int? AvatarId,
    string? PhotoBase64,
    string? PhotoExtension,
    string Email,
    Level Level,
    string BaseCountry,
    string BaseCity);
