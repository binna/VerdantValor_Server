using SharedLibrary.Models;

namespace SharedLibrary.Efcore.Repository;

public interface IUsersRepository
{
    Task<Users?> FindUserByEmailAsync(string email);
    Task<bool> ExistsUserAsync(string email);
    Task<int> SaveAsync(string email, string nickname, string password);
}