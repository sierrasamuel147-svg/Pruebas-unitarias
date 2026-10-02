using FluentAssertions;
using TestingPlayground.Collections;
using Xunit;

namespace TestingPlayground.Tests.Collections;

public class CollectionServiceTests
{
    private readonly CollectionService _service = new();

    // ---------- RemoveDuplicates ----------

    [Theory]
    [InlineData(new[] { 3, 1, 3, 2, 1 }, new[] { 3, 1, 2 })]
    [InlineData(new[] { -1, -1, 0, -2 }, new[] { -1, 0, -2 })]
    [InlineData(new[] { 7, 7, 7 }, new[] { 7 })]
    [InlineData(new[] { 5 }, new[] { 5 })]
    [InlineData(new int[0], new int[0])]
    public void RemoveDuplicates_KeepsFirstOccurrenceOrder(int[] numbers, int[] expected)
    {
        Assert.Equal(expected, _service.RemoveDuplicates(numbers));
    }

    [Fact]
    public void RemoveDuplicates_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.RemoveDuplicates(null!));
    }

    // ---------- Intersection ----------

    [Theory]
    [InlineData(new[] { 1, 2, 3, 4 }, new[] { 3, 4, 5, 6 }, new[] { 3, 4 })]
    [InlineData(new[] { 1, 1, 2, 2 }, new[] { 2, 1 }, new[] { 1, 2 })]
    [InlineData(new[] { 1, 2 }, new[] { 3, 4 }, new int[0])]
    [InlineData(new int[0], new[] { 1 }, new int[0])]
    public void Intersection_ReturnsDistinctCommonElementsInOrderOfFirst(
        int[] first, int[] second, int[] expected)
    {
        Assert.Equal(expected, _service.Intersection(first, second));
    }

    // ---------- Union ----------

    [Theory]
    [InlineData(new[] { 1, 2, 3, 4 }, new[] { 3, 4, 5, 6 }, new[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(new[] { 2, 2, 1 }, new[] { 1, 3, 3 }, new[] { 2, 1, 3 })]
    [InlineData(new int[0], new int[0], new int[0])]
    public void Union_ReturnsDistinctElementsOfFirstThenSecond(
        int[] first, int[] second, int[] expected)
    {
        Assert.Equal(expected, _service.Union(first, second));
    }

    // ---------- Difference ----------

    [Theory]
    [InlineData(new[] { 1, 2, 3, 4 }, new[] { 3, 4, 5, 6 }, new[] { 1, 2 })]
    [InlineData(new[] { 1, 1, 2 }, new int[0], new[] { 1, 2 })]
    [InlineData(new[] { 1, 2 }, new[] { 1, 2, 3 }, new int[0])]
    public void Difference_ReturnsDistinctElementsOfFirstNotInSecond(
        int[] first, int[] second, int[] expected)
    {
        Assert.Equal(expected, _service.Difference(first, second));
    }

    // ---------- SymmetricDifference ----------

    [Theory]
    [InlineData(new[] { 1, 2, 3, 4 }, new[] { 3, 4, 5, 6 }, new[] { 1, 2, 5, 6 })]
    [InlineData(new[] { 1, 1, 2 }, new[] { 2, 3, 3 }, new[] { 1, 3 })]
    [InlineData(new[] { 1, 2 }, new[] { 2, 1 }, new int[0])]
    [InlineData(new int[0], new[] { 4 }, new[] { 4 })]
    public void SymmetricDifference_ReturnsOnlyFirstThenOnlySecond(
        int[] first, int[] second, int[] expected)
    {
        Assert.Equal(expected, _service.SymmetricDifference(first, second));
    }

    [Fact]
    public void SetOperations_NullArguments_ThrowArgumentNullException()
    {
        var numbers = new[] { 1, 2 };

        Assert.Throws<ArgumentNullException>(() => _service.Intersection(null!, numbers));
        Assert.Throws<ArgumentNullException>(() => _service.Intersection(numbers, null!));
        Assert.Throws<ArgumentNullException>(() => _service.Union(null!, numbers));
        Assert.Throws<ArgumentNullException>(() => _service.Union(numbers, null!));
        Assert.Throws<ArgumentNullException>(() => _service.Difference(null!, numbers));
        Assert.Throws<ArgumentNullException>(() => _service.Difference(numbers, null!));
        Assert.Throws<ArgumentNullException>(() => _service.SymmetricDifference(null!, numbers));
        Assert.Throws<ArgumentNullException>(() => _service.SymmetricDifference(numbers, null!));
    }

    // ---------- GetEvenNumbers ----------

    [Theory]
    [InlineData(new[] { 1, 2, 3, 4, 5, 6 }, new[] { 2, 4, 6 })]
    [InlineData(new[] { -4, -3, 0, 7 }, new[] { -4, 0 })]
    [InlineData(new[] { 2, 2, 1 }, new[] { 2, 2 })]
    [InlineData(new[] { 1, 3, 5 }, new int[0])]
    [InlineData(new int[0], new int[0])]
    public void GetEvenNumbers_KeepsEvenNumbersInOriginalOrder(int[] numbers, int[] expected)
    {
        Assert.Equal(expected, _service.GetEvenNumbers(numbers));
    }

    // ---------- Average ----------

    [Theory]
    [InlineData(new[] { 1, 2, 3, 4 }, 2.5)]
    [InlineData(new[] { 5 }, 5.0)]
    [InlineData(new[] { -2, 2 }, 0.0)]
    [InlineData(new[] { -3, -4 }, -3.5)]
    public void Average_NonEmpty_ReturnsArithmeticMean(int[] numbers, double expected)
    {
        Assert.Equal(expected, _service.Average(numbers));
    }

    [Fact]
    public void Average_ValuesWhoseSumExceedsIntRange_DoesNotOverflow()
    {
        var result = _service.Average(new[] { int.MaxValue, int.MaxValue });

        Assert.Equal((double)int.MaxValue, result);
    }

    [Fact]
    public void Average_Empty_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _service.Average(Array.Empty<int>()));
    }

    // ---------- GetTop ----------

    [Theory]
    [InlineData(new[] { 5, 1, 9, 3 }, 2, new[] { 9, 5 })]
    [InlineData(new[] { 1, 3 }, 5, new[] { 3, 1 })]
    [InlineData(new[] { 5, 3, 5 }, 2, new[] { 5, 5 })]
    [InlineData(new[] { -1, -10, -5 }, 2, new[] { -1, -5 })]
    [InlineData(new[] { 4 }, 1, new[] { 4 })]
    [InlineData(new int[0], 3, new int[0])]
    public void GetTop_ReturnsUpToQuantityLargestInDescendingOrder(
        int[] numbers, int quantity, int[] expected)
    {
        Assert.Equal(expected, _service.GetTop(numbers, quantity));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetTop_QuantityNotPositive_ThrowsArgumentOutOfRangeException(int quantity)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => _service.GetTop(new[] { 1, 2, 3 }, quantity));

        Assert.Equal("quantity", exception.ParamName);
    }

    // ---------- FluentAssertions ----------
    // Equal/ContainInOrder check order; BeEquivalentTo ignores it.
    // Choosing one or the other documents whether order is part of the contract.

    [Fact]
    public void RemoveDuplicates_ResultIsOrderedUniqueSubsetOfInput()
    {
        var input = new[] { 3, 1, 3, 2, 1 };

        var result = _service.RemoveDuplicates(input);

        result.Should().Equal(3, 1, 2)
            .And.OnlyHaveUniqueItems()
            .And.BeSubsetOf(input);
    }

    [Fact]
    public void SymmetricDifference_HasSetSemanticsAndExcludesCommonElements()
    {
        var first = new[] { 1, 2, 3, 4 };
        var second = new[] { 3, 4, 5, 6 };

        var result = _service.SymmetricDifference(first, second);

        result.Should().BeEquivalentTo(new[] { 6, 5, 2, 1 });
        result.Should().NotIntersectWith(_service.Intersection(first, second));
    }

    [Fact]
    public void GetTop_ReturnsRequestedAmountInDescendingOrder()
    {
        var result = _service.GetTop(new[] { 5, 9, 1, 7, 3 }, 3);

        result.Should().HaveCount(3)
            .And.BeInDescendingOrder()
            .And.ContainInOrder(9, 7, 5)
            .And.NotContain(1);
    }

    [Theory]
    [InlineData(new[] { 1, 2, 3, 10 })]
    [InlineData(new[] { -5, 0, 5 })]
    [InlineData(new[] { 42 })]
    public void Average_IsAlwaysBetweenMinimumAndMaximum(int[] numbers)
    {
        var result = _service.Average(numbers);

        result.Should().BeInRange(numbers.Min(), numbers.Max());
    }

    [Fact]
    public void Average_NonTerminatingDecimal_IsApproximatelyCorrect()
    {
        var result = _service.Average(new[] { 1, 1, 2 });

        result.Should().BeApproximately(1.333, 0.001);
    }

    [Fact]
    public void Average_Empty_ThrowsWithExplanation()
    {
        Action act = () => _service.Average(Array.Empty<int>());

        act.Should().Throw<InvalidOperationException>().WithMessage("*empty*");
    }

    [Fact]
    public void GetTop_ZeroQuantity_ThrowsReportingParameterAndValue()
    {
        Action act = () => _service.GetTop(new[] { 1, 2 }, 0);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("quantity")
            .Which.ActualValue.Should().Be(0);
    }
}
