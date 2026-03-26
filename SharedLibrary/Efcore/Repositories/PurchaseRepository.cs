using Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Efcore.Repositories;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly IDbContextFactory<AppDbContext> mDbContextFactory;
    
    public PurchaseRepository(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        mDbContextFactory = dbContextFactory;
    }
    
    public async Task<int> CountAsync(ulong userId, int storeId)
    {
        await using var dbContext = await mDbContextFactory.CreateDbContextAsync();
        return await dbContext.Purchase
            .CountAsync(p => p.UserId == userId && p.StoreId == storeId);
    }
    
    public async Task<bool> ExistsAsync(ulong userId, int storeId)
    {
        await using var dbContext = await mDbContextFactory.CreateDbContextAsync();
        return await dbContext.Purchase
            .AnyAsync(p => p.UserId == userId && p.StoreId == storeId);
    }

    public async Task<Purchase> AddAsync(int storeId, ulong userId)
    {
        await using var dbContext = await mDbContextFactory.CreateDbContextAsync();
        var purchase = new Purchase(storeId, userId);
        await dbContext.Purchase.AddAsync(purchase);
        await dbContext.SaveChangesAsync();
        return purchase;
    }

    public async Task<Purchase> MarkAsCompletedAsync(Purchase purchase)
    {
        await using var dbContext = await mDbContextFactory.CreateDbContextAsync();
        purchase.MarkAsCompleted();
        await dbContext.Purchase.AddAsync(purchase);
        await dbContext.SaveChangesAsync();
        return purchase;
    }
}