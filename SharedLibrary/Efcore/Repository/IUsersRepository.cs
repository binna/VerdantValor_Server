using Common.Models;

namespace Efcore.Repository;

public interface IUsersRepository
{
    Task<Users?> FindUserByEmailAsync(string email);
    Task<bool> ExistsUserAsync(string email);
    Task AddAsync(string email, string nickname, string password);
}