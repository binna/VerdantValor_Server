using Common.Models;

namespace Efcore.Repositories;

public interface IGameUserRepository
{
    Task<GameUser?> FindByEmailAsync(string email);
    Task<GameUser?> FindByUserIdAsync(ulong userId);
    Task<bool> ExistsAsync(string email);
    Task AddAsync(string email, string nickname, string password);
    Task AddAsync(GameUser user);
}