namespace TestingPlayground.Advanced.Users;

public interface IPasswordHasher
{
    string Hash(string password);
}
