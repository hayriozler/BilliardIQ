namespace BilliardIQ.Mobile.Utilities;

public static class InviteLink
{
    public const string Scheme = "billiardiq";
    public const string Host = "invite";

    public static string? Pending { get; private set; }

    public static event EventHandler? Received;

    public static string Build(string code) => $"{Scheme}://{Host}/{Uri.EscapeDataString(code)}";

    public static string? Parse(string? link)
    {
        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals(Scheme, StringComparison.OrdinalIgnoreCase) ||
            !uri.Host.Equals(Host, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var code = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));
        return code.Length is > 0 and <= 32 ? code.ToUpperInvariant() : null;
    }

    public static void Offer(string? link)
    {
        if (Parse(link) is not { } code)
        {
            return;
        }

        Pending = code;
        Received?.Invoke(null, EventArgs.Empty);
    }

    public static string? Take()
    {
        var code = Pending;
        Pending = null;
        return code;
    }
}
