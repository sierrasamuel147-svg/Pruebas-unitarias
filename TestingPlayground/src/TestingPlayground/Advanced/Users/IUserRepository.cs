namespace TestingPlayground.Advanced.Users;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email);

    Task AddAsync(User user);
}
