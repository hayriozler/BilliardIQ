namespace Scoreboard.Client.Services;

public class RemoteSyncOptions
{
    public bool Enabled { get; set; }

    public string ClientId { get; set; } = "";

    public int TableNo { get; set; }

    public string BaseUrl { get; set; } = "";

    public int PollSeconds { get; set; } = 15;

    /// <summary>
    /// A remote server is configured: sync is on and it is told where to go and who this table is. Then players 1 and 2
    /// come from that server (its system players); otherwise they are created locally when the database is set up.
    /// </summary>
    public bool IsConfigured =>
        Enabled && !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ClientId) && TableNo > 0;
}
