using Common.Models;

namespace Efcore.Repositories;

public interface IGameUserRepository
{
    public Task<GameUser?> FindByEmailAsync(string email);
    public Task<GameUser?> FindByUserIdAsync(ulong userId);
    public Task<bool> ExistsAsync(string email);
    public Task AddAsync(string email, string nickname, string password);
    public Task AddAsync(GameUser user);
}