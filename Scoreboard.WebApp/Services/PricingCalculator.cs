namespace Scoreboard.WebApp.Services;

public record BillingResult(int BilledMinutes, decimal Amount);

/// <summary>Turns a session's price snapshot + elapsed time into billed minutes and an amount.</summary>
public static class PricingCalculator
{
    private const int MaxMinutes = 7 * 24 * 60;

    public static int ElapsedMinutes(TableSession session, DateTimeOffset now)
    {
        var paused = session.PausedMinutes;
        if (session.PausedAt is { } pausedAt)
        {
            paused += (int)Math.Round((now - pausedAt).TotalMinutes);
        }

        return Math.Max(0, (int)Math.Floor((now - session.OpenedAt).TotalMinutes) - paused);
    }

    public static int BillableMinutes(int elapsedMinutes, int roundingMinutes, int minimumMinutes)
    {
        var minutes = Math.Max(elapsedMinutes, minimumMinutes);
        if (roundingMinutes > 1)
        {
            minutes = (int)Math.Ceiling(minutes / (double)roundingMinutes) * roundingMinutes;
        }

        return minutes;
    }

    public static BillingResult Calculate(
        PricingSnapshot pricing, DateTimeOffset openedAt, int elapsedMinutes, int roundingMinutes, int minimumMinutes, TimeZoneInfo timeZone)
    {
        var billed = BillableMinutes(elapsedMinutes, roundingMinutes, minimumMinutes);
        return new BillingResult(billed, Amount(pricing, openedAt, billed, timeZone));
    }

    /// <summary>Live estimate shown on the dashboard while a session is running.</summary>
    public static decimal Estimate(
        PricingSnapshot pricing, DateTimeOffset openedAt, int elapsedMinutes, int roundingMinutes, int minimumMinutes, TimeZoneInfo timeZone) =>
        Calculate(pricing, openedAt, elapsedMinutes, roundingMinutes, minimumMinutes, timeZone).Amount;

    private static decimal Amount(PricingSnapshot pricing, DateTimeOffset openedAt, int minutes, TimeZoneInfo timeZone)
    {
        if (pricing.TimeSlots.Count == 0)
        {
            return Math.Round(pricing.DefaultHourlyRate * minutes / 60m, 2);
        }

        var total = 0m;
        for (var i = 0; i < Math.Min(minutes, MaxMinutes); i++)
        {
            var local = TimeZoneInfo.ConvertTime(openedAt.AddMinutes(i), timeZone);
            total += RateAt(pricing, local) / 60m;
        }

        return Math.Round(total, 2);
    }

    private static decimal RateAt(PricingSnapshot pricing, DateTimeOffset local)
    {
        var time = TimeOnly.FromDateTime(local.DateTime);
        foreach (var slot in pricing.TimeSlots)
        {
            var wraps = slot.To <= slot.From; // e.g. 22:00 → 02:00
            if (!wraps)
            {
                if (slot.DaysOfWeek.Contains(local.DayOfWeek) && time >= slot.From && time < slot.To)
                {
                    return slot.HourlyRate;
                }

                continue;
            }

            if (time >= slot.From && slot.DaysOfWeek.Contains(local.DayOfWeek))
            {
                return slot.HourlyRate;
            }

            if (time < slot.To && slot.DaysOfWeek.Contains(local.AddDays(-1).DayOfWeek))
            {
                return slot.HourlyRate;
            }
        }

        return pricing.DefaultHourlyRate;
    }

    public static TimeZoneInfo ResolveTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
