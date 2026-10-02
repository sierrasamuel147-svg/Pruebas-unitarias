using System.Text.RegularExpressions;

namespace TestingPlayground.Patterns;

public class WildcardMatcher
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    public bool IsMatch(string input, string pattern, bool ignoreCase = true)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(pattern);

        var options = RegexOptions.CultureInvariant | RegexOptions.Singleline;

        if (ignoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        return Regex.IsMatch(input, ToRegexPattern(pattern), options, RegexTimeout);
    }

    private static string ToRegexPattern(string wildcardPattern)
    {
        var escaped = Regex.Escape(wildcardPattern)
            .Replace(@"\*", ".*")
            .Replace(@"\?", ".");

        return $@"^{escaped}\z";
    }
}
