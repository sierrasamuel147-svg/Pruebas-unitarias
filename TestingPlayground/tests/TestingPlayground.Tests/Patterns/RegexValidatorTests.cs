using FluentAssertions;
using TestingPlayground.Patterns;
using Xunit;

namespace TestingPlayground.Tests.Patterns;

public class RegexValidatorTests
{
    private readonly RegexValidator _validator = new();

    // ---------- Email ----------

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last+tag@sub.domain.co")]
    [InlineData("USER_1%x@dominio.org")]
    public void IsValidEmail_WellFormed_ReturnsTrue(string value)
    {
        Assert.True(_validator.IsValidEmail(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("userexample.com")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    [InlineData("user@example")]
    [InlineData("user@example.c")]
    [InlineData("user name@example.com")]
    [InlineData(" user@example.com")]
    [InlineData("user@example.com ")]
    [InlineData("user@example.com\n")]
    public void IsValidEmail_Malformed_ReturnsFalse(string? value)
    {
        Assert.False(_validator.IsValidEmail(value!));
    }

    // ---------- Colombian mobile ----------

    [Theory]
    [InlineData("3001234567")]
    [InlineData("+57 3001234567")]
    [InlineData("+573001234567")]
    [InlineData("3209876543")]
    public void IsColombianMobile_ValidNumbers_ReturnsTrue(string value)
    {
        Assert.True(_validator.IsColombianMobile(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2001234567")]
    [InlineData("300123456")]
    [InlineData("30012345678")]
    [InlineData("300123456a")]
    [InlineData("300 123 4567")]
    [InlineData("57 3001234567")]
    [InlineData("+1 3001234567")]
    [InlineData("+57  3001234567")]
    [InlineData("3001234567\n")]
    public void IsColombianMobile_InvalidNumbers_ReturnsFalse(string? value)
    {
        Assert.False(_validator.IsColombianMobile(value!));
    }

    // ---------- Date format ----------

    [Theory]
    [InlineData("01/01/2026")]
    [InlineData("31/12/1999")]
    [InlineData("02/10/2026")]
    public void IsValidDateFormat_DdMmYyyy_ReturnsTrue(string value)
    {
        Assert.True(_validator.IsValidDateFormat(value));
    }

    [Theory]
    [InlineData("31/02/2026")]
    [InlineData("29/02/2025")]
    [InlineData("31/04/2026")]
    public void IsValidDateFormat_NonExistentDateWithValidShape_ReturnsTrueBecauseOnlyFormatIsChecked(string value)
    {
        Assert.True(_validator.IsValidDateFormat(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1/1/2026")]
    [InlineData("00/01/2026")]
    [InlineData("32/01/2026")]
    [InlineData("15/00/2026")]
    [InlineData("15/13/2026")]
    [InlineData("2026/10/02")]
    [InlineData("02-10-2026")]
    [InlineData("02/10/26")]
    [InlineData("02/10/2026\n")]
    public void IsValidDateFormat_WrongShape_ReturnsFalse(string? value)
    {
        Assert.False(_validator.IsValidDateFormat(value!));
    }

    // ---------- Strong password ----------

    [Theory]
    [InlineData("Samuel123!")]
    [InlineData("Abcdef1@")]
    [InlineData("XyZ12345$%")]
    public void IsStrongPassword_MeetsAllRules_ReturnsTrue(string value)
    {
        Assert.True(_validator.IsStrongPassword(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Abcde1!")]
    [InlineData("abcdefg1!")]
    [InlineData("ABCDEFG1!")]
    [InlineData("Abcdefgh!")]
    [InlineData("Abcdefg12")]
    [InlineData("Abcdef1#")]
    [InlineData("Abc def1!")]
    [InlineData("Ñandu123!")]
    [InlineData("Samuel123!\n")]
    public void IsStrongPassword_BreaksAnyRule_ReturnsFalse(string? value)
    {
        Assert.False(_validator.IsStrongPassword(value!));
    }

    // ---------- Credit card format ----------

    [Theory]
    [InlineData("4111111111111111")]
    [InlineData("4111 1111 1111 1111")]
    [InlineData("4111-1111-1111-1111")]
    [InlineData("4222222222222")]
    [InlineData("4111111111111111111")]
    public void IsCreditCardFormatValid_13To19DigitsWithOptionalSeparators_ReturnsTrue(string value)
    {
        Assert.True(_validator.IsCreditCardFormatValid(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("411111111111")]
    [InlineData("41111111111111111111")]
    [InlineData("4111A11111111111")]
    [InlineData(" 4111111111111111")]
    [InlineData("4111111111111111\n")]
    public void IsCreditCardFormatValid_WrongShape_ReturnsFalse(string? value)
    {
        Assert.False(_validator.IsCreditCardFormatValid(value!));
    }

    [Fact]
    public void IsCreditCardFormatValid_DoesNotVerifyChecksum()
    {
        const string wrongChecksum = "4111111111111112";

        Assert.True(_validator.IsCreditCardFormatValid(wrongChecksum));
        Assert.False(_validator.PassesLuhn(wrongChecksum));
    }

    // ---------- Luhn ----------

    [Theory]
    [InlineData("4111111111111111")]
    [InlineData("4111 1111 1111 1111")]
    [InlineData("4111-1111-1111-1111")]
    [InlineData("5555555555554444")]
    [InlineData("378282246310005")]
    [InlineData("4222222222222")]
    public void PassesLuhn_ValidChecksum_ReturnsTrue(string cardNumber)
    {
        Assert.True(_validator.PassesLuhn(cardNumber));
    }

    [Theory]
    [InlineData("4111111111111112")]
    [InlineData("5555555555554445")]
    public void PassesLuhn_InvalidChecksum_ReturnsFalse(string cardNumber)
    {
        Assert.False(_validator.PassesLuhn(cardNumber));
    }

    [Theory]
    [InlineData("4111A11111111111")]
    [InlineData("4111.1111.1111.1111")]
    [InlineData("4111_1111_1111_1111")]
    [InlineData("4111/1111/1111/1111")]
    public void PassesLuhn_CharactersOtherThanDigitsSpacesOrHyphens_ReturnsFalse(string cardNumber)
    {
        Assert.False(_validator.PassesLuhn(cardNumber));
    }

    [Theory]
    [InlineData("000000000000")]
    [InlineData("00004111111111111111")]
    public void PassesLuhn_LengthOutside13To19_ReturnsFalseEvenIfChecksumWouldPass(string cardNumber)
    {
        Assert.False(_validator.PassesLuhn(cardNumber));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PassesLuhn_NullOrBlank_ReturnsFalse(string? cardNumber)
    {
        Assert.False(_validator.PassesLuhn(cardNumber!));
    }

    // ---------- FluentAssertions ----------
    // For booleans the gain is the "because" clause: a failure explains the rule that broke.

    [Fact]
    public void IsValidDateFormat_ImpossibleDate_IsAcceptedByDesign()
    {
        _validator.IsValidDateFormat("31/02/2026")
            .Should().BeTrue("the method validates only the DD/MM/YYYY shape, not whether the date exists");
    }

    [Fact]
    public void IsColombianMobile_TrailingNewline_IsRejected()
    {
        _validator.IsColombianMobile("3001234567\n")
            .Should().BeFalse("the pattern is anchored with \\z, which does not allow a trailing newline");
    }

    [Fact]
    public void PassesLuhn_LetterInsideNumber_IsRejectedInsteadOfIgnored()
    {
        _validator.PassesLuhn("4111A11111111111")
            .Should().BeFalse("only spaces and hyphens may be removed before running Luhn");
    }
}
