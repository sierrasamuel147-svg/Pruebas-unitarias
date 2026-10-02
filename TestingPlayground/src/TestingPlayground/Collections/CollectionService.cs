namespace TestingPlayground.Collections;

public class CollectionService
{
    public IReadOnlyList<int> RemoveDuplicates(IEnumerable<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);

        var seen = new HashSet<int>();
        var firstOccurrences = new List<int>();

        foreach (var number in numbers)
        {
            if (seen.Add(number))
            {
                firstOccurrences.Add(number);
            }
        }

        return firstOccurrences.AsReadOnly();
    }

    public IReadOnlyList<int> Intersection(IEnumerable<int> first, IEnumerable<int> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return first.Intersect(second).ToList().AsReadOnly();
    }

    public IReadOnlyList<int> Union(IEnumerable<int> first, IEnumerable<int> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return first.Union(second).ToList().AsReadOnly();
    }

    public IReadOnlyList<int> Difference(IEnumerable<int> first, IEnumerable<int> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return first.Except(second).ToList().AsReadOnly();
    }

    public IReadOnlyList<int> SymmetricDifference(IEnumerable<int> first, IEnumerable<int> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var firstItems = first.ToList();
        var secondItems = second.ToList();

        return firstItems.Except(secondItems)
            .Concat(secondItems.Except(firstItems))
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<int> GetEvenNumbers(IEnumerable<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);

        return numbers.Where(number => number % 2 == 0).ToList().AsReadOnly();
    }

    public double Average(IEnumerable<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);

        var items = numbers.ToList();

        if (items.Count == 0)
        {
            throw new InvalidOperationException("Cannot calculate the average of an empty collection.");
        }

        return items.Average();
    }

    public IReadOnlyList<int> GetTop(IEnumerable<int> numbers, int quantity)
    {
        ArgumentNullException.ThrowIfNull(numbers);

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be greater than zero.");
        }

        return numbers
            .OrderByDescending(number => number)
            .Take(quantity)
            .ToList()
            .AsReadOnly();
    }
}
