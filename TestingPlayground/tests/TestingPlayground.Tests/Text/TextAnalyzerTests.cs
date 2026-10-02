using FluentAssertions;
using TestingPlayground.Text;
using Xunit;

namespace TestingPlayground.Tests.Text;

public class TextAnalyzerTests
{
    private readonly TextAnalyzer _analyzer = new();

    // ---------- NormalizeWhitespace ----------

    [Theory]
    [InlineData("   Hola     mundo   ", "Hola mundo")]
    [InlineData("Hola\t\tmundo", "Hola mundo")]
    [InlineData("Hola\r\n\nmundo", "Hola mundo")]
    [InlineData(" \t uno \n dos \t tres \r\n", "uno dos tres")]
    [InlineData("Hola", "Hola")]
    [InlineData("", "")]
    [InlineData("   \t\n  ", "")]
    public void NormalizeWhitespace_VariousInputs_CollapsesAndTrimsWhitespace(string text, string expected)
    {
        var result = _analyzer.NormalizeWhitespace(text);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeWhitespace_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _analyzer.NormalizeWhitespace(null!));
    }

    // ---------- IsPalindrome ----------

    [Theory]
    [InlineData("Anita lava la tina")]
    [InlineData("A man, a plan, a canal: Panama!")]
    [InlineData("Oso")]
    [InlineData("12321")]
    [InlineData("a")]
    [InlineData("Ana, 1 1 ana")]
    public void IsPalindrome_Palindromes_ReturnsTrue(string text)
    {
        Assert.True(_analyzer.IsPalindrome(text));
    }

    [Theory]
    [InlineData("Hola mundo")]
    [InlineData("12345")]
    [InlineData("ab")]
    public void IsPalindrome_NonPalindromes_ReturnsFalse(string text)
    {
        Assert.False(_analyzer.IsPalindrome(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!! ,,, ...")]
    public void IsPalindrome_NoLettersOrDigitsAfterNormalization_ReturnsFalse(string text)
    {
        Assert.False(_analyzer.IsPalindrome(text));
    }

    [Fact]
    public void IsPalindrome_AccentedLetters_AreComparedLiterally()
    {
        // Accents are not stripped: 'á' and 'a' are different characters.
        Assert.False(_analyzer.IsPalindrome("Dábale arroz a la zorra el abad"));
    }

    [Fact]
    public void IsPalindrome_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _analyzer.IsPalindrome(null!));
    }

    // ---------- CountWords ----------

    [Theory]
    [InlineData("Hola mundo desde C#", 4)]
    [InlineData("uno, dos; tres.", 3)]
    [InlineData("año 2026 niño", 3)]
    [InlineData("Ñandú café", 2)]
    [InlineData("123 456", 2)]
    [InlineData("e-mail", 2)]
    [InlineData("", 0)]
    [InlineData("   \t\n", 0)]
    [InlineData("!!! ???", 0)]
    public void CountWords_VariousInputs_CountsUnicodeLetterAndDigitRuns(string text, int expected)
    {
        Assert.Equal(expected, _analyzer.CountWords(text));
    }

    [Fact]
    public void CountWords_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _analyzer.CountWords(null!));
    }

    // ---------- ContainsAllWords ----------

    [Theory]
    [InlineData("Hola Mundo desde CSharp", new[] { "hola", "MUNDO" }, true)]
    [InlineData("Hola Mundo desde CSharp", new[] { "hola", "adiós" }, false)]
    [InlineData("Canción de cuna", new[] { "CANCIÓN" }, true)]
    [InlineData("Testing en .NET", new[] { "test" }, false)]
    [InlineData("Hola mundo", new[] { "  hola  " }, true)]
    [InlineData("Hola mundo", new string[0], true)]
    [InlineData("", new[] { "hola" }, false)]
    public void ContainsAllWords_VariousInputs_MatchesWholeWordsIgnoringCase(
        string text, string[] requiredWords, bool expected)
    {
        Assert.Equal(expected, _analyzer.ContainsAllWords(text, requiredWords));
    }

    [Fact]
    public void ContainsAllWords_NullText_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _analyzer.ContainsAllWords(null!, new[] { "hola" }));
    }

    [Fact]
    public void ContainsAllWords_NullRequiredWords_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _analyzer.ContainsAllWords("hola", null!));
    }

    [Fact]
    public void ContainsAllWords_RequiredWordsContainNull_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => _analyzer.ContainsAllWords("hola", new[] { "hola", null! }));

        Assert.Equal("requiredWords", exception.ParamName);
    }

    // ---------- ExtractHashtags ----------

    [Fact]
    public void ExtractHashtags_TextWithHashtags_ReturnsThemInOrderOfAppearance()
    {
        var result = _analyzer.ExtractHashtags("Aprendiendo #dotnet y #Testing");

        Assert.Equal(new[] { "#dotnet", "#Testing" }, result);
    }

    [Fact]
    public void ExtractHashtags_UnicodeDigitsAndUnderscore_AreSupported()
    {
        var result = _analyzer.ExtractHashtags("Viaje #año_2026 con #café");

        Assert.Equal(new[] { "#año_2026", "#café" }, result);
    }

    [Theory]
    [InlineData("Sin etiquetas")]
    [InlineData("# solo")]
    [InlineData("")]
    public void ExtractHashtags_NoHashtags_ReturnsEmpty(string text)
    {
        Assert.Empty(_analyzer.ExtractHashtags(text));
    }

    [Fact]
    public void ExtractHashtags_HashtagEndsAtPunctuation()
    {
        var hashtag = Assert.Single(_analyzer.ExtractHashtags("Me encanta #xunit!"));

        Assert.Equal("#xunit", hashtag);
    }

    [Fact]
    public void ExtractHashtags_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _analyzer.ExtractHashtags(null!));
    }

    // ---------- HasBalancedBrackets ----------

    [Theory]
    [InlineData("(hola [mundo] {test})")]
    [InlineData("{[()]}")]
    [InlineData("()[]{}")]
    [InlineData("sin brackets")]
    [InlineData("")]
    public void HasBalancedBrackets_BalancedText_ReturnsTrue(string text)
    {
        Assert.True(_analyzer.HasBalancedBrackets(text));
    }

    [Theory]
    [InlineData("(hola [mundo)")]
    [InlineData("([)]")]
    [InlineData("(")]
    [InlineData(")")]
    [InlineData(")(")]
    [InlineData("{[}")]
    [InlineData("(()")]
    public void HasBalancedBrackets_UnbalancedOrMisorderedText_ReturnsFalse(string text)
    {
        Assert.False(_analyzer.HasBalancedBrackets(text));
    }

    [Fact]
    public void HasBalancedBrackets_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _analyzer.HasBalancedBrackets(null!));
    }

    // ---------- FluentAssertions ----------
    // Chained string assertions, ordered collections and exception details
    // that would need several separate Assert calls in plain xUnit.

    [Fact]
    public void NormalizeWhitespace_MixedWhitespace_ProducesCleanSingleSpacedText()
    {
        var result = _analyzer.NormalizeWhitespace(" \t uno \n dos \t tres \r\n");

        result.Should().StartWith("uno")
            .And.EndWith("tres")
            .And.NotContain("  ")
            .And.Be("uno dos tres");
    }

    [Fact]
    public void ExtractHashtags_SeveralHashtags_ReturnsThemInOrderWithHashPrefix()
    {
        var result = _analyzer.ExtractHashtags("#uno, luego #dos y al final #tres.");

        // xUnit: Assert.Equal(new[] { "#uno", "#dos", "#tres" }, result);
        result.Should().HaveCount(3)
            .And.ContainInOrder("#uno", "#dos", "#tres")
            .And.OnlyContain(tag => tag.StartsWith("#"));
    }

    [Fact]
    public void NormalizeWhitespace_Null_ThrowsReportingParameterName()
    {
        Action act = () => _analyzer.NormalizeWhitespace(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("text");
    }

    [Fact]
    public void ContainsAllWords_RequiredWordsContainNull_ThrowsWithDescriptiveMessage()
    {
        Action act = () => _analyzer.ContainsAllWords("hola", new[] { "hola", null! });

        act.Should().Throw<ArgumentException>()
            .WithParameterName("requiredWords")
            .WithMessage("*cannot contain null*");
    }
}
