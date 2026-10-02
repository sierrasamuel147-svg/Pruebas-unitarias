using System.Globalization;
using FluentAssertions;
using FluentAssertions.Extensions;
using TestingPlayground.Dates;
using Xunit;

namespace TestingPlayground.Tests.Dates;

// Reference calendar: 2026-10-02 is a Friday, 2026-10-03 Saturday, 2026-10-05 Monday.
public class DateTimeServiceTests
{
    private readonly DateTimeService _service = new();

    private static DateTime D(string value) =>
        DateTime.ParseExact(
            value,
            new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm" },
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);

    // ---------- CalculateAge ----------

    [Theory]
    [InlineData("2000-01-15", "2026-01-15", 26)]
    [InlineData("2000-01-15", "2026-01-14", 25)]
    [InlineData("2000-06-01", "2026-10-02", 26)]
    [InlineData("2000-12-31", "2026-10-02", 25)]
    [InlineData("2026-10-02", "2026-10-02", 0)]
    public void CalculateAge_RegularBirthDates_ConsidersWhetherBirthdayHasOccurred(
        string birthDate, string referenceDate, int expected)
    {
        Assert.Equal(expected, _service.CalculateAge(D(birthDate), D(referenceDate)));
    }

    [Theory]
    [InlineData("2001-02-28", 0)]
    [InlineData("2001-03-01", 1)]
    [InlineData("2004-02-28", 3)]
    [InlineData("2004-02-29", 4)]
    [InlineData("2026-02-28", 25)]
    [InlineData("2026-03-01", 26)]
    public void CalculateAge_BornOnFebruary29_HasBirthdayOnMarch1InNonLeapYears(
        string referenceDate, int expected)
    {
        Assert.Equal(expected, _service.CalculateAge(D("2000-02-29"), D(referenceDate)));
    }

    [Fact]
    public void CalculateAge_BirthDateAfterReferenceDate_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => _service.CalculateAge(D("2026-10-03"), D("2026-10-02")));

        Assert.Equal("birthDate", exception.ParamName);
    }

    // ---------- IsWithinRange ----------

    [Theory]
    [InlineData("2026-10-02 10:00", true)]
    [InlineData("2026-10-02 11:00", true)]
    [InlineData("2026-10-02 12:00", true)]
    [InlineData("2026-10-02 09:59", false)]
    [InlineData("2026-10-02 12:01", false)]
    public void IsWithinRange_RangeIsInclusive(string value, bool expected)
    {
        var result = _service.IsWithinRange(D(value), D("2026-10-02 10:00"), D("2026-10-02 12:00"));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsWithinRange_StartEqualsEnd_MatchesThatExactInstant()
    {
        var instant = D("2026-10-02 10:00");

        Assert.True(_service.IsWithinRange(instant, instant, instant));
    }

    [Fact]
    public void IsWithinRange_StartAfterEnd_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => _service.IsWithinRange(D("2026-10-02 11:00"), D("2026-10-02 12:00"), D("2026-10-02 10:00")));
    }

    // ---------- RangesOverlap ----------

    [Theory]
    [InlineData("10:00", "11:00", "11:00", "12:00", false)]
    [InlineData("11:00", "12:00", "10:00", "11:00", false)]
    [InlineData("10:00", "11:00", "13:00", "14:00", false)]
    [InlineData("10:00", "11:00", "10:30", "12:00", true)]
    [InlineData("10:00", "11:00", "10:00", "11:00", true)]
    [InlineData("09:00", "13:00", "10:00", "11:00", true)]
    [InlineData("10:00", "11:00", "10:59", "11:30", true)]
    public void RangesOverlap_HalfOpenRanges_DetectsOverlap(
        string start1, string end1, string start2, string end2, bool expected)
    {
        var result = _service.RangesOverlap(At(start1), At(end1), At(start2), At(end2));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("11:00", "10:00", "10:00", "11:00", "end1")]
    [InlineData("10:00", "10:00", "10:00", "11:00", "end1")]
    [InlineData("10:00", "11:00", "12:00", "11:00", "end2")]
    [InlineData("10:00", "11:00", "12:00", "12:00", "end2")]
    public void RangesOverlap_EmptyOrInvertedRange_ThrowsArgumentException(
        string start1, string end1, string start2, string end2, string expectedParamName)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => _service.RangesOverlap(At(start1), At(end1), At(start2), At(end2)));

        Assert.Equal(expectedParamName, exception.ParamName);
    }

    private static DateTime At(string time) => D($"2026-10-02 {time}");

    // ---------- CountBusinessDays ----------

    [Theory]
    [InlineData("2026-10-05", "2026-10-09", 5)]
    [InlineData("2026-10-05", "2026-10-11", 5)]
    [InlineData("2026-10-02", "2026-10-05", 2)]
    [InlineData("2026-10-03", "2026-10-04", 0)]
    [InlineData("2026-10-05", "2026-10-05", 1)]
    [InlineData("2026-10-03", "2026-10-03", 0)]
    [InlineData("2026-10-01", "2026-10-31", 22)]
    public void CountBusinessDays_InclusiveRange_CountsOnlyMondayToFriday(
        string start, string end, int expected)
    {
        Assert.Equal(expected, _service.CountBusinessDays(D(start), D(end)));
    }

    [Fact]
    public void CountBusinessDays_StartAfterEnd_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _service.CountBusinessDays(D("2026-10-09"), D("2026-10-05")));
    }

    // ---------- AddBusinessDays ----------

    [Theory]
    [InlineData("2026-10-02", 1, "2026-10-05")]
    [InlineData("2026-10-05", 5, "2026-10-12")]
    [InlineData("2026-10-02", 6, "2026-10-12")]
    [InlineData("2026-10-03", 1, "2026-10-05")]
    [InlineData("2026-10-04", 1, "2026-10-05")]
    public void AddBusinessDays_SkipsWeekends(string start, int businessDays, string expected)
    {
        Assert.Equal(D(expected), _service.AddBusinessDays(D(start), businessDays));
    }

    [Theory]
    [InlineData("2026-10-01")]
    [InlineData("2026-10-03")]
    public void AddBusinessDays_Zero_ReturnsStartUnchangedEvenOnWeekend(string start)
    {
        Assert.Equal(D(start), _service.AddBusinessDays(D(start), 0));
    }

    [Fact]
    public void AddBusinessDays_PreservesTimeOfDay()
    {
        var result = _service.AddBusinessDays(D("2026-10-02 15:30"), 1);

        Assert.Equal(D("2026-10-05 15:30"), result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void AddBusinessDays_Negative_ThrowsArgumentOutOfRangeException(int businessDays)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => _service.AddBusinessDays(D("2026-10-02"), businessDays));

        Assert.Equal("businessDays", exception.ParamName);
    }

    // ---------- Difference ----------

    [Fact]
    public void Difference_EndAfterStart_ReturnsElapsedTime()
    {
        var result = _service.Difference(D("2026-10-02 10:00"), D("2026-10-03 12:30"));

        Assert.Equal(new TimeSpan(1, 2, 30, 0), result);
    }

    [Fact]
    public void Difference_StartEqualsEnd_ReturnsZero()
    {
        var instant = D("2026-10-02 10:00");

        Assert.Equal(TimeSpan.Zero, _service.Difference(instant, instant));
    }

    [Fact]
    public void Difference_EndBeforeStart_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => _service.Difference(D("2026-10-02 10:00"), D("2026-10-02 09:59")));

        Assert.Equal("end", exception.ParamName);
    }

    // ---------- FluentAssertions ----------
    // FluentAssertions.Extensions makes dates and durations read like prose.

    [Fact]
    public void AddBusinessDays_FromFridayAfternoon_LandsOnMondaySameTime()
    {
        var friday = 2.October(2026).At(15, 30);

        var result = _service.AddBusinessDays(friday, 1);

        result.Should().Be(5.October(2026).At(15, 30));
        result.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(10)]
    public void AddBusinessDays_PositiveAmount_NeverLandsOnWeekend(int businessDays)
    {
        var result = _service.AddBusinessDays(2.October(2026), businessDays);

        result.DayOfWeek.Should().NotBe(DayOfWeek.Saturday).And.NotBe(DayOfWeek.Sunday);
        result.Should().BeAfter(2.October(2026));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void CountBusinessDays_AnySevenConsecutiveDays_ContainsExactlyFive(int offset)
    {
        var start = 5.October(2026).AddDays(offset);

        _service.CountBusinessDays(start, start.AddDays(6)).Should().Be(5);
    }

    [Fact]
    public void Difference_ReturnsReadableDuration()
    {
        var result = _service.Difference(2.October(2026).At(10, 0), 2.October(2026).At(12, 30));

        // xUnit: Assert.Equal(new TimeSpan(2, 30, 0), result);
        result.Should().Be(2.Hours().And(30.Minutes()));
    }

    [Fact]
    public void CalculateAge_BirthDateAfterReference_ThrowsReportingParameterName()
    {
        Action act = () => _service.CalculateAge(3.October(2026), 2.October(2026));

        act.Should().Throw<ArgumentException>().WithParameterName("birthDate");
    }

    [Fact]
    public void AddBusinessDays_Negative_ThrowsReportingActualValue()
    {
        Action act = () => _service.AddBusinessDays(2.October(2026), -3);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("businessDays")
            .Which.ActualValue.Should().Be(-3);
    }
}
