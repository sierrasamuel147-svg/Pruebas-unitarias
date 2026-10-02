namespace TestingPlayground.Dates;

public class DateTimeService
{
    public int CalculateAge(DateTime birthDate, DateTime referenceDate)
    {
        if (birthDate > referenceDate)
        {
            throw new ArgumentException("Birth date cannot be after the reference date.", nameof(birthDate));
        }

        var age = referenceDate.Year - birthDate.Year;

        // AddYears maps Feb 29 to Feb 28 in non-leap years, so leap-day births
        // complete a year on Mar 1 of non-leap years.
        if (birthDate.Date > referenceDate.Date.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    public bool IsWithinRange(DateTime value, DateTime start, DateTime end)
    {
        EnsureStartNotAfterEnd(start, end);

        return value >= start && value <= end;
    }

    public bool RangesOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
    {
        EnsureValidHalfOpenRange(start1, end1, nameof(end1));
        EnsureValidHalfOpenRange(start2, end2, nameof(end2));

        return start1 < end2 && start2 < end1;
    }

    public int CountBusinessDays(DateTime start, DateTime end)
    {
        EnsureStartNotAfterEnd(start, end);

        var businessDays = 0;

        for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
        {
            if (IsBusinessDay(day))
            {
                businessDays++;
            }
        }

        return businessDays;
    }

    public DateTime AddBusinessDays(DateTime start, int businessDays)
    {
        if (businessDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(businessDays), businessDays, "Business days cannot be negative.");
        }

        var result = start;
        var remaining = businessDays;

        while (remaining > 0)
        {
            result = result.AddDays(1);

            if (IsBusinessDay(result))
            {
                remaining--;
            }
        }

        return result;
    }

    public TimeSpan Difference(DateTime start, DateTime end)
    {
        if (end < start)
        {
            throw new ArgumentException("End cannot be before start.", nameof(end));
        }

        return end - start;
    }

    private static bool IsBusinessDay(DateTime day) =>
        day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    private static void EnsureStartNotAfterEnd(DateTime start, DateTime end)
    {
        if (start > end)
        {
            throw new ArgumentException("Start cannot be after end.", nameof(start));
        }
    }

    private static void EnsureValidHalfOpenRange(DateTime start, DateTime end, string paramName)
    {
        if (end <= start)
        {
            throw new ArgumentException("Range end must be after its start.", paramName);
        }
    }
}
