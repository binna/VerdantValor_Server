using Common.Models;
using Common.Types;
using Microsoft.AspNetCore.Http;

namespace Efcore.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly IHttpContextAccessor mHttpContextAccessor;
    
    public InventoryRepository(IHttpContextAccessor httpContextAccessor)
    {
        mHttpContextAccessor = httpContextAccessor;
    }
    
    public async Task AddAsync(int itemId, int amount, ulong userId, ServerDateTime? expiredAt = null)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        var inventories = new List<Inventory>(amount);
        
        for (var i = 0; i < amount; i++)
            inventories.Add(new Inventory(itemId, userId, expiredAt));
        
        await dbContext.Inventory.AddRangeAsync(inventories);
    }
}