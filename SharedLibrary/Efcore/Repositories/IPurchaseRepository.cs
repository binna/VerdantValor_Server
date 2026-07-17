using Common.Models;

namespace Efcore.Repositories;

public interface IPurchaseRepository
{
    Task<int> CountAsync(ulong userId, int storeId);
    Task<Purchase> AddAndSaveAsync(int storeId, ulong userId);
    Task MarkAsCompletedAsync(ulong purchaseId);
}