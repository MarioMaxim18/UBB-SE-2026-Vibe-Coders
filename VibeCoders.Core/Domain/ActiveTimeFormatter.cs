using System.Globalization;

namespace VibeCoders.Domain;

/// <summary>
/// Formats active training duration as an H:MM:SS string.
/// Hours are not zero-padded so that multi-day totals remain readable
/// (e.g., "127:04:30"). Minutes and seconds are always two digits.
/// </summary>
public static class ActiveTimeFormatter
{
    /// <summary>
    /// Converts a non-negative <see cref="TimeSpan"/> to H:MM:SS.
    /// Negative values are clamped to zero. Sub-second fractions are
    /// truncated (floor), not rounded.
    /// </summary>
    public static string ToHourMinuteSecond(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        var totalSeconds = (long)Math.Floor(duration.TotalSeconds);
        var hours = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        var seconds = totalSeconds % 60;
        return string.Create(CultureInfo.InvariantCulture, $"{hours}:{minutes:D2}:{seconds:D2}");
    }
}
