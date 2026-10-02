namespace TestingPlayground.Advanced.Users;

public record User(
    Guid Id,
    string Name,
    string Email,
    string PasswordHash,
    DateTime CreatedAt
);
