using System.Text.Json;

namespace Zeymera.BillardIQ.Client.Models;

public record ScoreboardCommandMessage(ScoreboardCommand Command, JsonElement? Payload = null);

public record PlayerUpsertPayload(int Id, string? Nickname = null, string? Name = null, string? PhotoBase64 = null, string? PhotoExtension = null, string? Avatar = null, int? TeamId = null, int? ShortcutNumber = null);

public record TeamAddPayload(int Id, string Name);

public record SetPlayerPayload(int Id, int? ShortcutNumber = null);
