using System.Text.Json;

namespace Scoreboard.Client.Models;

public static class ScoreboardCommandParser
{
    public static IReadOnlyList<ScoreboardCommandMessage> TryParse(string text)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            var root = doc.RootElement;

            if (string.Equals(GetStringProperty(root, "type"), "state", StringComparison.OrdinalIgnoreCase))
            {
                return [new ScoreboardCommandMessage(ScoreboardCommand.GetState)];
            }

            if (TryGetProperty(root, "commands", out var commandsProperty) && commandsProperty.ValueKind == JsonValueKind.Array)
            {
                var messages = new List<ScoreboardCommandMessage>();
                foreach (var item in commandsProperty.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object && TryParseSingle(item) is { } message)
                    {
                        messages.Add(message);
                    }
                    else if (item.ValueKind == JsonValueKind.String &&
                        item.GetString() is { } commandName &&
                        Enum.TryParse<ScoreboardCommand>(commandName, ignoreCase: true, out var command))
                    {
                        messages.Add(new ScoreboardCommandMessage(command));
                    }
                }

                return messages;
            }

            return TryParseSingle(root) is { } single ? [single] : [];
        }
        catch (JsonException)
        {
            return Enum.TryParse<ScoreboardCommand>(text, ignoreCase: true, out var bareCommand)
                ? [new ScoreboardCommandMessage(bareCommand)]
                : [];
        }
    }

    private static ScoreboardCommandMessage? TryParseSingle(JsonElement obj)
    {
        if (!TryGetProperty(obj, "command", out var commandProperty) ||
            commandProperty.ValueKind != JsonValueKind.String ||
            commandProperty.GetString() is not { } commandName ||
            !Enum.TryParse<ScoreboardCommand>(commandName, ignoreCase: true, out var command))
        {
            return null;
        }

        JsonElement? payload = TryGetProperty(obj, "payload", out var payloadProperty) ? payloadProperty.Clone() : null;
        return new ScoreboardCommandMessage(command, payload);
    }

    private static bool TryGetProperty(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var property in obj.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? GetStringProperty(JsonElement obj, string name) =>
        TryGetProperty(obj, name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
