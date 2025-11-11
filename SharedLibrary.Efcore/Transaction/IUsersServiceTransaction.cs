namespace SharedLibrary.Efcore.Transaction;

public interface IUsersServiceTransaction
{
    Task<int> CreateUserAsync(string email, string nickname, string password);
}