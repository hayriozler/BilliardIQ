using System.Text.Json.Serialization;

namespace BilliardIQ.Mobile.Services;

public sealed class ScoreBoardRequest
{
    [JsonPropertyName("type")] public string Type { get; set; } = "command";
    [JsonPropertyName("commands")] public ScoreBoardCommand[] Commands { get; set; } = [];
}

public sealed class ScoreBoardCommand(string commandName, object? payload = null)
{
    [JsonPropertyName("command")]
    public string CommandName { get; } = commandName;

    [JsonPropertyName("payload")]
    public object? Payload { get; } = payload;
}
