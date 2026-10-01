namespace Scoreboard.WebApp.Domain;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public sealed class GlobalFilterAttribute(string? property = null) : Attribute
{
    public string? Property { get; } = property;

    public bool IncludeNull { get; init; }

    public string? IncludeWhen { get; init; }
}
