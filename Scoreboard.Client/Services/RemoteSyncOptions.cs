namespace Scoreboard.Client.Services;

public class RemoteSyncOptions
{
    public bool Enabled { get; set; }

    public string ClientId { get; set; } = "";

    public string BaseUrl { get; set; } = "";

    public int PollSeconds { get; set; } = 15;
}
