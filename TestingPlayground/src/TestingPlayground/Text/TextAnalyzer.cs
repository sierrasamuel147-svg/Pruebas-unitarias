using System.Text.RegularExpressions;

namespace TestingPlayground.Text;

public class TextAnalyzer
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly Regex WhitespaceRegex =
        new(@"\s+", RegexOptions.Compiled, RegexTimeout);

    // A word is a run of Unicode letters, combining marks or digits: "C#" counts as the word "C".
    private static readonly Regex WordRegex =
        new(@"[\p{L}\p{M}\p{N}]+", RegexOptions.Compiled, RegexTimeout);

    private static readonly Regex HashtagRegex =
        new(@"#[\p{L}\p{M}\p{N}_]+", RegexOptions.Compiled, RegexTimeout);

    private static readonly Dictionary<char, char> ClosingToOpening = new()
    {
        [')'] = '(',
        [']'] = '[',
        ['}'] = '{',
    };

    public string NormalizeWhitespace(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return WhitespaceRegex.Replace(text.Trim(), " ");
    }

    public bool IsPalindrome(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var normalized = text
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();

        if (normalized.Length == 0)
        {
            return false;
        }

        for (int left = 0, right = normalized.Length - 1; left < right; left++, right--)
        {
            if (normalized[left] != normalized[right])
            {
                return false;
            }
        }

        return true;
    }

    public int CountWords(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return WordRegex.Matches(text).Count;
    }

    public bool ContainsAllWords(string text, IEnumerable<string> requiredWords)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(requiredWords);

        var wordsInText = WordRegex.Matches(text)
            .Select(match => match.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var requiredWord in requiredWords)
        {
            if (requiredWord is null)
            {
                throw new ArgumentException("Required words cannot contain null values.", nameof(requiredWords));
            }

            if (!wordsInText.Contains(requiredWord.Trim()))
            {
                return false;
            }
        }

        return true;
    }

    public IReadOnlyList<string> ExtractHashtags(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return HashtagRegex.Matches(text)
            .Select(match => match.Value)
            .ToList()
            .AsReadOnly();
    }

    public bool HasBalancedBrackets(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var openBrackets = new Stack<char>();

        foreach (var character in text)
        {
            if (IsOpeningBracket(character))
            {
                openBrackets.Push(character);
            }
            else if (ClosingToOpening.TryGetValue(character, out var expectedOpening))
            {
                if (openBrackets.Count == 0 || openBrackets.Pop() != expectedOpening)
                {
                    return false;
                }
            }
        }

        return openBrackets.Count == 0;
    }

    private static bool IsOpeningBracket(char character) =>
        ClosingToOpening.ContainsValue(character);
}
