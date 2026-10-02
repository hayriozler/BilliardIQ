namespace Scoreboard.WebApp.Responses;

public static class ChangeState
{
    public const string Changed = "changed";
    public const string Deleted = "deleted";
}

public record ChangeGroupDto(string EntityType, string State, IReadOnlyList<object>? Items, IReadOnlyList<int>? Ids);

public record ChangeSetDto(bool Full, List<ChangeGroupDto> Changes);
