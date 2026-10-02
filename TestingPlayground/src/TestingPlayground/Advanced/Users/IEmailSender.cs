namespace TestingPlayground.Advanced.Users;

public interface IEmailSender
{
    Task SendWelcomeEmailAsync(
        string email,
        string name
    );
}
