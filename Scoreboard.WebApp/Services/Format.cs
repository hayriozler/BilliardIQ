using System.Globalization;

namespace Scoreboard.WebApp.Services;

/// <summary>Display helpers shared by the admin pages.</summary>
public static class Format
{
    private static readonly CultureInfo _culture = new("tr-TR");

    public static string Money(decimal amount, string currency = "TRY")
    {
        var symbol = currency switch { "TRY" => "₺", "EUR" => "€", "USD" => "$", _ => currency };
        return $"{amount.ToString("N2", _culture)} {symbol}";
    }

    public static string Clock(double totalSeconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
        return $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
    }

    public static string Minutes(int minutes) =>
        minutes < 60 ? $"{minutes} dk" : $"{minutes / 60} sa {minutes % 60:00} dk";

    public static string Time(DateTimeOffset value, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTime(value, zone).ToString("HH:mm", _culture);

    public static string DateTime(DateTimeOffset value, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTime(value, zone).ToString("dd.MM.yyyy HH:mm", _culture);

    public static string TableType(TableType type) => type switch
    {
        Domain.TableType.Match284 => "Maç masası (284)",
        Domain.TableType.Size250 => "250 masa",
        Domain.TableType.Size230 => "230 masa",
        Domain.TableType.Pool => "Pool",
        Domain.TableType.Snooker => "Snooker",
        _ => type.ToString()
    };

    public static string TableStatus(TableStatus status) => status switch
    {
        Domain.TableStatus.Available => "Boş",
        Domain.TableStatus.InUse => "Dolu",
        Domain.TableStatus.Reserved => "Rezerve",
        Domain.TableStatus.Maintenance => "Bakımda",
        Domain.TableStatus.OutOfService => "Kapalı",
        _ => status.ToString()
    };

    public static string Role(StaffRole role) => role switch
    {
        StaffRole.Owner => "Sahip",
        StaffRole.Manager => "Yönetici",
        StaffRole.Cashier => "Kasiyer",
        StaffRole.Waiter => "Garson",
        StaffRole.Referee => "Hakem",
        _ => role.ToString()
    };

    public static string PaymentMethod(PaymentMethod method) => method switch
    {
        Domain.PaymentMethod.Cash => "Nakit",
        Domain.PaymentMethod.Card => "Kart",
        Domain.PaymentMethod.Transfer => "Havale",
        Domain.PaymentMethod.PrepaidBalance => "Bakiye",
        Domain.PaymentMethod.Complimentary => "İkram",
        _ => method.ToString()
    };

    public static string Day(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Pzt",
        DayOfWeek.Tuesday => "Sal",
        DayOfWeek.Wednesday => "Çar",
        DayOfWeek.Thursday => "Per",
        DayOfWeek.Friday => "Cum",
        DayOfWeek.Saturday => "Cmt",
        _ => "Paz"
    };
}
