using FluentAssertions;
using Moq;
using TestingPlayground.Advanced.Common;
using TestingPlayground.Advanced.Users;
using TestingPlayground.Patterns;
using Xunit;

namespace TestingPlayground.Tests.Advanced.Users;

public class UserRegistrationServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 2, 15, 30, 0, DateTimeKind.Utc);

    private const string Name = "Samuel";
    private const string RawEmail = "  SAMUEL@GMAIL.COM  ";
    private const string NormalizedEmail = "samuel@gmail.com";
    private const string Password = "Samuel123!";
    private const string PasswordHash = "HASH_TEST";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IClock> _clock = new();
    private readonly UserRegistrationService _service;

    public UserRegistrationServiceTests()
    {
        _clock.Setup(clock => clock.UtcNow).Returns(Now);
        _passwordHasher.Setup(hasher => hasher.Hash(Password)).Returns(PasswordHash);
        _users.Setup(users => users.ExistsByEmailAsync(It.IsAny<string>())).ReturnsAsync(false);

        // RegexValidator is pure logic, so the real implementation is used instead of a mock.
        _service = new UserRegistrationService(
            _users.Object,
            _passwordHasher.Object,
            _emailSender.Object,
            _clock.Object,
            new RegexValidator());
    }

    // ---------- Successful registration ----------

    [Fact]
    public async Task RegisterAsync_ValidData_NormalizesEmailAndCompletesRegistration()
    {
        User? savedUser = null;
        _users
            .Setup(users => users.AddAsync(It.IsAny<User>()))
            .Callback<User>(user => savedUser = user)
            .Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(Name, RawEmail, Password);

        result.Should().BeEquivalentTo(new
        {
            Name,
            Email = NormalizedEmail,
            PasswordHash,
            CreatedAt = Now,
        });
        result.Id.Should().NotBeEmpty();
        savedUser.Should().BeSameAs(result);

        _users.Verify(users => users.ExistsByEmailAsync(NormalizedEmail), Times.Once);
        _passwordHasher.Verify(hasher => hasher.Hash(Password), Times.Once);
        _users.Verify(users => users.AddAsync(result), Times.Once);
        _emailSender.Verify(sender => sender.SendWelcomeEmailAsync(NormalizedEmail, Name), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_NeverStoresPlainPassword()
    {
        User? savedUser = null;
        _users
            .Setup(users => users.AddAsync(It.IsAny<User>()))
            .Callback<User>(user => savedUser = user)
            .Returns(Task.CompletedTask);

        await _service.RegisterAsync(Name, RawEmail, Password);

        savedUser.Should().NotBeNull();
        savedUser!.PasswordHash.Should().Be(PasswordHash).And.NotBe(Password);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_TakesCreatedAtFromClock()
    {
        var result = await _service.RegisterAsync(Name, RawEmail, Password);

        result.CreatedAt.Should().Be(new DateTime(2026, 10, 2, 15, 30, 0, DateTimeKind.Utc))
            .And.BeIn(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData("  TEST@MAIL.COM  ", "test@mail.com")]
    [InlineData("Mixed.Case@Example.Org", "mixed.case@example.org")]
    [InlineData("\tuser@site.co\n", "user@site.co")]
    [InlineData("already@normal.com", "already@normal.com")]
    public async Task RegisterAsync_EmailVariants_UsesNormalizedEmailEverywhere(string email, string expected)
    {
        var result = await _service.RegisterAsync(Name, email, Password);

        result.Email.Should().Be(expected);
        _users.Verify(users => users.ExistsByEmailAsync(expected), Times.Once);
        _emailSender.Verify(
            sender => sender.SendWelcomeEmailAsync(It.Is<string>(address => address == expected), Name),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_NameWithSurroundingSpaces_StoresAndGreetsTrimmedName()
    {
        var result = await _service.RegisterAsync("  Samuel  ", RawEmail, Password);

        result.Name.Should().Be("Samuel");
        _emailSender.Verify(sender => sender.SendWelcomeEmailAsync(NormalizedEmail, "Samuel"), Times.Once);
    }

    // ---------- Duplicate email ----------

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ThrowsInvalidOperationException()
    {
        _users.Setup(users => users.ExistsByEmailAsync(NormalizedEmail)).ReturnsAsync(true);

        Func<Task> act = () => _service.RegisterAsync(Name, RawEmail, Password);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already exists*");
        _users.Verify(users => users.ExistsByEmailAsync(NormalizedEmail), Times.Once);
        _passwordHasher.Verify(hasher => hasher.Hash(It.IsAny<string>()), Times.Never);
        _users.Verify(users => users.AddAsync(It.IsAny<User>()), Times.Never);
        _emailSender.Verify(sender => sender.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ---------- Invalid input: repository is never queried ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_NameIsEmpty_ThrowsArgumentException(string? name)
    {
        Func<Task> act = () => _service.RegisterAsync(name!, RawEmail, Password);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("name");
        VerifyNothingWasPersistedOrSent();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_EmailIsMissing_ThrowsArgumentException(string? email)
    {
        Func<Task> act = () => _service.RegisterAsync(Name, email!, Password);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("email")
            .WithMessage("*required*");
        VerifyNothingWasPersistedOrSent();
    }

    [Theory]
    [InlineData("no-es-un-email")]
    [InlineData("samuel@")]
    [InlineData("samuel@gmail")]
    [InlineData("samuel @gmail.com")]
    public async Task RegisterAsync_EmailIsInvalid_ThrowsArgumentException(string email)
    {
        Func<Task> act = () => _service.RegisterAsync(Name, email, Password);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("email")
            .WithMessage("*invalid format*");
        VerifyNothingWasPersistedOrSent();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Corta1!")]
    [InlineData("sinmayuscula1!")]
    [InlineData("SINMINUSCULA1!")]
    [InlineData("SinNumeros!!")]
    [InlineData("SinEspecial12")]
    public async Task RegisterAsync_PasswordIsWeak_ThrowsArgumentException(string? password)
    {
        Func<Task> act = () => _service.RegisterAsync(Name, RawEmail, password!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("password");
        VerifyNothingWasPersistedOrSent();
    }

    // ---------- Helpers ----------

    private void VerifyNothingWasPersistedOrSent()
    {
        _users.Verify(users => users.ExistsByEmailAsync(It.IsAny<string>()), Times.Never);
        _passwordHasher.Verify(hasher => hasher.Hash(It.IsAny<string>()), Times.Never);
        _users.Verify(users => users.AddAsync(It.IsAny<User>()), Times.Never);
        _emailSender.Verify(sender => sender.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
