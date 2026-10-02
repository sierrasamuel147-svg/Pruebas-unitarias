using FluentAssertions;
using TestingPlayground.Patterns;
using Xunit;

namespace TestingPlayground.Tests.Patterns;

public class WildcardMatcherTests
{
    private readonly WildcardMatcher _matcher = new();

    [Theory]
    [InlineData("documento.pdf", "*.pdf")]
    [InlineData("factura.pdf", "*.pdf")]
    [InlineData(".pdf", "*.pdf")]
    [InlineData("image-01.png", "image-??.png")]
    [InlineData("abc", "abc")]
    [InlineData("", "*")]
    [InlineData("report[1].csv", "report[?].csv")]
    [InlineData("a+b(c).txt", "a+b(c).txt")]
    [InlineData("línea1\nlínea2", "*")]
    public void IsMatch_MatchingInput_ReturnsTrue(string input, string pattern)
    {
        Assert.True(_matcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("factura.pdf.txt", "*.pdf")]
    [InlineData("image-001.png", "image-??.png")]
    [InlineData("image-1.png", "image-??.png")]
    [InlineData("abcd", "abc")]
    [InlineData("xabc", "abc")]
    [InlineData("", "?")]
    [InlineData("aXb", "a.b")]
    [InlineData("factura.pdf\n", "*.pdf")]
    public void IsMatch_NonMatchingInput_ReturnsFalse(string input, string pattern)
    {
        Assert.False(_matcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("Factura.PDF", "*.pdf", true, true)]
    [InlineData("Factura.PDF", "*.pdf", false, false)]
    [InlineData("factura.pdf", "*.pdf", false, true)]
    [InlineData("IMAGE-01.png", "image-??.png", false, false)]
    public void IsMatch_IgnoreCaseFlag_ControlsCaseSensitivity(
        string input, string pattern, bool ignoreCase, bool expected)
    {
        Assert.Equal(expected, _matcher.IsMatch(input, pattern, ignoreCase));
    }

    [Fact]
    public void IsMatch_IgnoresCaseByDefault()
    {
        Assert.True(_matcher.IsMatch("REPORTE.CSV", "reporte.csv"));
    }

    [Fact]
    public void IsMatch_NullInput_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => _matcher.IsMatch(null!, "*"));

        Assert.Equal("input", exception.ParamName);
    }

    [Fact]
    public void IsMatch_NullPattern_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => _matcher.IsMatch("abc", null!));

        Assert.Equal("pattern", exception.ParamName);
    }

    // ---------- FluentAssertions ----------

    [Fact]
    public void IsMatch_FilteringFileNames_SelectsOnlyMatchingFiles()
    {
        var files = new[] { "a.pdf", "B.PDF", "notes.txt", "pdf", "c.pdf.bak" };

        var pdfs = files.Where(file => _matcher.IsMatch(file, "*.pdf"));

        pdfs.Should().BeEquivalentTo("a.pdf", "B.PDF");
    }

    [Fact]
    public void IsMatch_CaseSensitiveFiltering_ExcludesDifferentCase()
    {
        var files = new[] { "a.pdf", "B.PDF" };

        var pdfs = files.Where(file => _matcher.IsMatch(file, "*.pdf", ignoreCase: false));

        pdfs.Should().ContainSingle().Which.Should().Be("a.pdf");
    }
}
