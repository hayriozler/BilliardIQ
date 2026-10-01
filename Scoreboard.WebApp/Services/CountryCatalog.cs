namespace Scoreboard.WebApp.Services;

public static class CountryCatalog
{
    public sealed record Entry(string Code, string Tr, string En, string Nl, string Language, string Currency, string TimeZone)
    {
        public string NameIn(string language) => language switch
        {
            "tr" => Tr,
            "nl" => Nl,
            _ => En
        };

        public bool HasName(string? name) =>
            name is not null && new[] { Code, Tr, En, Nl }.Any(n => string.Equals(n, name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static readonly IReadOnlyList<Entry> All =
    [
        new("TR", "Türkiye", "Türkiye", "Turkije", "tr", "TRY", "Europe/Istanbul"),
        new("NL", "Hollanda", "Netherlands", "Nederland", "nl", "EUR", "Europe/Amsterdam"),
        new("BE", "Belçika", "Belgium", "België", "nl", "EUR", "Europe/Brussels"),
        new("DE", "Almanya", "Germany", "Duitsland", "en", "EUR", "Europe/Berlin"),
        new("FR", "Fransa", "France", "Frankrijk", "en", "EUR", "Europe/Paris"),
        new("ES", "İspanya", "Spain", "Spanje", "en", "EUR", "Europe/Madrid"),
        new("IT", "İtalya", "Italy", "Italië", "en", "EUR", "Europe/Rome"),
        new("GB", "Birleşik Krallık", "United Kingdom", "Verenigd Koninkrijk", "en", "GBP", "Europe/London"),
        new("US", "ABD", "United States", "Verenigde Staten", "en", "USD", "America/New_York")
    ];

    public static readonly IReadOnlyList<string> Currencies = ["TRY", "EUR", "USD", "GBP"];

    public static Entry? Find(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : All.FirstOrDefault(e => string.Equals(e.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool IsCurrency(string? code) => code is not null && Currencies.Contains(code.Trim().ToUpperInvariant());

    public static string DefaultFor(string language) => language switch
    {
        "tr" => "TR",
        "nl" => "NL",
        _ => "GB"
    };
}
