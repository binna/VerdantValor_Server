using Common.Manager;
using Common.Models;
using Efcore.Repositories;
using Shared.GameData;
using Shared.Types;

namespace WebServer.Services;

public class ItemService
{
    private readonly ILogger<ItemService> mLogger;
    private readonly IInventoryRepository mInventoryRepository;
    
    public ItemService(
        ILogger<ItemService> logger,
        IInventoryRepository inventoryRepository)
    {
        mLogger = logger;
        mInventoryRepository = inventoryRepository;
    }
    
    public async Task<bool> GainItem(Store.Item[] items, GameUser user)
    {
        foreach (var item in items)
        {
            if (GameDataManager.ItemTable.TryGet(item.Id, out var value))
            {
                switch (value.ItemKind)
                {
                    case EItemKind.Gold:
                        if (!user.GainGold(item.Amount))
                            return false;
                        break;
                    case EItemKind.Exp:
                        if (!user.GainExp(item.Amount))
                            return false;
                        break;
                    case EItemKind.Potion:
                        if (item.Amount <= 0)
                            return false;
                        await mInventoryRepository.AddAsync(item.Id, item.Amount, user.UserId);
                        break;
                }
            }
        }
        return true;
    }
}