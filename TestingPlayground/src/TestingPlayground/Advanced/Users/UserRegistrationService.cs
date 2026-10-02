using TestingPlayground.Advanced.Common;
using TestingPlayground.Patterns;

namespace TestingPlayground.Advanced.Users;

public class UserRegistrationService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly RegexValidator _validator;

    public UserRegistrationService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IClock clock,
        RegexValidator validator)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(validator);

        _users = users;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _clock = clock;
        _validator = validator;
    }

    public async Task<User> RegisterAsync(string name, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var normalizedEmail = NormalizeEmail(email);

        if (!_validator.IsValidEmail(normalizedEmail))
        {
            throw new ArgumentException("Email has an invalid format.", nameof(email));
        }

        if (!_validator.IsStrongPassword(password))
        {
            throw new ArgumentException("Password does not meet the strength requirements.", nameof(password));
        }

        if (await _users.ExistsByEmailAsync(normalizedEmail))
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var passwordHash = _passwordHasher.Hash(password);

        var user = new User(Guid.NewGuid(), name.Trim(), normalizedEmail, passwordHash, _clock.UtcNow);

        await _users.AddAsync(user);
        await _emailSender.SendWelcomeEmailAsync(user.Email, user.Name);

        return user;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
