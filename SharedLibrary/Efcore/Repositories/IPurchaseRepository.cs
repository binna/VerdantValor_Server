using Common.Models;

namespace Efcore.Repositories;

public interface IPurchaseRepository
{
    public Task<int> CountAsync(ulong userId, int storeId);
    public Task<bool> ExistsAsync(ulong userId, int storeId);
    public Task<Purchase> AddAsync(int storeId, ulong userId);
    public Task<Purchase> MarkAsCompletedAsync(Purchase purchase);
}