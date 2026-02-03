using Common.Models;

namespace Efcore.Repositories;

public interface IUsersRepository
{
    Task<Users?> FindUserByEmailAsync(string email);
    Task<bool> ExistsUserAsync(string email);
    Task AddAsync(string email, string nickname, string password);
}