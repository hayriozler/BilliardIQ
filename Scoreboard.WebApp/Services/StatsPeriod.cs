namespace Scoreboard.WebApp.Services;

public sealed record StatsPeriod(string Kind = "6m", DateOnly? From = null, DateOnly? To = null)
{
    public static StatsPeriod Default { get; } = new();

    public DateTimeOffset? Since => Kind switch
    {
        "30" => DateTimeOffset.UtcNow.AddDays(-30),
        "90" => DateTimeOffset.UtcNow.AddDays(-90),
        "6m" => DateTimeOffset.UtcNow.AddMonths(-6),
        "year" => new DateTimeOffset(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, TimeSpan.Zero),
        "custom" when From is { } from => new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
        _ => null
    };

    public DateTimeOffset? Until => Kind == "custom" && To is { } to
        ? new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
        : null;
}
