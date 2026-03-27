using Common.Types;

namespace Efcore.Repositories;

public interface IInventoryRepository
{
    public Task AddAsync(int itemId, int amount, ulong userId, ServerDateTime? expiredAt = null);
}