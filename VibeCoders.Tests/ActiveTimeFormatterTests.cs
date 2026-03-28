using VibeCoders.Domain;

namespace VibeCoders.Tests;

/// <summary>
/// Tests for <see cref="ActiveTimeFormatter.ToHourMinuteSecond"/>.
/// </summary>
public sealed class ActiveTimeFormatterTests
{
    [Fact]
    public void Zero_returns_0_00_00()
    {
        Assert.Equal("0:00:00", ActiveTimeFormatter.ToHourMinuteSecond(TimeSpan.Zero));
    }

    [Fact]
    public void Negative_duration_is_clamped_to_zero()
    {
        Assert.Equal("0:00:00", ActiveTimeFormatter.ToHourMinuteSecond(TimeSpan.FromMinutes(-5)));
    }

    [Theory]
    [InlineData(0, 0, 1, "0:00:01")]
    [InlineData(0, 1, 0, "0:01:00")]
    [InlineData(1, 0, 0, "1:00:00")]
    [InlineData(1, 30, 45, "1:30:45")]
    [InlineData(0, 59, 59, "0:59:59")]
    public void Typical_durations_are_formatted_correctly(
        int hours, int minutes, int seconds, string expected)
    {
        var duration = new TimeSpan(hours, minutes, seconds);
        Assert.Equal(expected, ActiveTimeFormatter.ToHourMinuteSecond(duration));
    }

    [Fact]
    public void Large_duration_shows_unpadded_hours()
    {
        var duration = TimeSpan.FromHours(127) + TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(30);
        Assert.Equal("127:04:30", ActiveTimeFormatter.ToHourMinuteSecond(duration));
    }

    [Fact]
    public void Sub_second_fractions_are_truncated_not_rounded()
    {
        var duration = TimeSpan.FromSeconds(59.999);
        Assert.Equal("0:00:59", ActiveTimeFormatter.ToHourMinuteSecond(duration));
    }

    [Fact]
    public void One_millisecond_under_a_minute_stays_at_59_seconds()
    {
        var duration = TimeSpan.FromMinutes(1) - TimeSpan.FromMilliseconds(1);
        Assert.Equal("0:00:59", ActiveTimeFormatter.ToHourMinuteSecond(duration));
    }
}
