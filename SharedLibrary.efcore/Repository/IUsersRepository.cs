using SharedLibrary.Models;

namespace SharedLibrary.Efcore.Repository;

public interface IUsersRepository
{
    Task<Users?> FindUserByEmailAsync(string email);
    Task<bool> ExistsUserAsync(string email);
    Task AddAsync(string email, string nickname, string password);
    Task<int> SaveAsync();
}