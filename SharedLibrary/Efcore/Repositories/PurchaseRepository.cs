using Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Efcore.Repositories;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly IDbContextFactory<AppDbContext> mDbContextFactory;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    
    public PurchaseRepository(
        IDbContextFactory<AppDbContext> dbContextFactory, 
        IHttpContextAccessor httpContextAccessor)
    {
        mDbContextFactory = dbContextFactory;
        mHttpContextAccessor = httpContextAccessor;
    }
    
    public async Task<int> CountAsync(ulong userId, int storeId)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        return await dbContext.Purchase
            .CountAsync(p => p.UserId == userId && p.StoreId == storeId);
    }

    public async Task<Purchase> AddAndSaveAsync(int storeId, ulong userId)
    {
        await using var dbContext = await mDbContextFactory.CreateDbContextAsync();
        var purchase = new Purchase(storeId, userId);
        await dbContext.Purchase.AddAsync(purchase);
        await dbContext.SaveChangesAsync();
        return purchase;
    }
    
    public async Task MarkAsCompletedAsync(ulong purchaseId)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        var purchase = await dbContext.Purchase.FirstOrDefaultAsync(p => p.Id == purchaseId);
        purchase?.MarkAsCompleted();
    }
}