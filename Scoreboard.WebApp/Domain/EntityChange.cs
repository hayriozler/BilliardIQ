namespace Scoreboard.WebApp.Domain;

public class EntityChange
{
    public long Seq { get; set; }
    public int OrganizationId { get; set; }
    public string EntityName { get; set; } = default!;
    public int EntityId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
