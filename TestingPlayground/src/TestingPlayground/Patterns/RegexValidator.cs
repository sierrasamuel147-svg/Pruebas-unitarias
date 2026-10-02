using System.Text.RegularExpressions;

namespace TestingPlayground.Patterns;

public class RegexValidator
{
    private const int MinCardDigits = 13;
    private const int MaxCardDigits = 19;

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly Regex EmailRegex = Compile(
        @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\z");

    private static readonly Regex ColombianMobileRegex = Compile(
        @"^(?:\+57\s?)?3\d{9}\z");

    private static readonly Regex DateFormatRegex = Compile(
        @"^(0[1-9]|[12]\d|3[01])\/(0[1-9]|1[0-2])\/\d{4}\z");

    private static readonly Regex StrongPasswordRegex = Compile(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}\z");

    private static readonly Regex CreditCardFormatRegex = Compile(
        @"^(?:\d[ -]*?){13,19}\z");

    public bool IsValidEmail(string value) => Matches(EmailRegex, value);

    public bool IsColombianMobile(string value) => Matches(ColombianMobileRegex, value);

    /// <summary>
    /// Validates only the DD/MM/YYYY shape; impossible dates such as 31/02/2026 still pass.
    /// </summary>
    public bool IsValidDateFormat(string value) => Matches(DateFormatRegex, value);

    public bool IsStrongPassword(string value) => Matches(StrongPasswordRegex, value);

    /// <summary>
    /// Validates only the shape of a card number; use <see cref="PassesLuhn"/> for the checksum.
    /// </summary>
    public bool IsCreditCardFormatValid(string value) => Matches(CreditCardFormatRegex, value);

    public bool PassesLuhn(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return false;
        }

        var digits = cardNumber.Replace(" ", string.Empty).Replace("-", string.Empty);

        if (digits.Length is < MinCardDigits or > MaxCardDigits || !digits.All(IsAsciiDigit))
        {
            return false;
        }

        return CalculateLuhnSum(digits) % 10 == 0;
    }

    private static int CalculateLuhnSum(string digits)
    {
        var sum = 0;
        var doubleDigit = false;

        for (var index = digits.Length - 1; index >= 0; index--)
        {
            var digit = digits[index] - '0';

            if (doubleDigit)
            {
                digit *= 2;

                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            doubleDigit = !doubleDigit;
        }

        return sum;
    }

    private static bool IsAsciiDigit(char character) => character is >= '0' and <= '9';

    private static bool Matches(Regex regex, string value) =>
        !string.IsNullOrWhiteSpace(value) && regex.IsMatch(value);

    private static Regex Compile(string pattern) =>
        new(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, RegexTimeout);
}
