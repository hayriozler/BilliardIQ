namespace Zeymera.BillardIQ.WebApp.Requests;

public record RegisterScoreboardClientRequest(string? Name, int? ClubId, int? TableNumber);
