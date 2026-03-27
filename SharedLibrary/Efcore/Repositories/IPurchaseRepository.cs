using Common.Models;

namespace Efcore.Repositories;

public interface IPurchaseRepository
{
    public Task<int> CountAsync(ulong userId, int storeId);
    public Task<Purchase> AddAndSaveAsync(int storeId, ulong userId);
    public Task MarkAsCompletedAsync(ulong purchaseId);
}