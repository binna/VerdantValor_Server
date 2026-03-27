using Common.Concurrency;
using Common.Manager;
using Common.Models;
using Common.Web;
using Efcore.Repositories;
using Shared.GameData;
using Shared.Types;

namespace WebServer.Services;

public class StoreService
{
    private readonly ILogger<StoreService> mLogger;
    private readonly IPurchaseRepository mPurchaseRepository;
    private readonly IGameUserRepository mGameUserRepository;
    private readonly IInventoryRepository mInventoryRepository;
    private readonly DistributedLock mDistributedLock;
    
    public StoreService(
        ILogger<StoreService> logger,
        IPurchaseRepository purchaseRepository,
        IGameUserRepository gameUserRepository, 
        IInventoryRepository inventoryRepository,
        DistributedLock distributedLock)
    {
        mLogger = logger;
        mPurchaseRepository = purchaseRepository; 
        mGameUserRepository = gameUserRepository;
        mInventoryRepository = inventoryRepository;
        mDistributedLock = distributedLock;
    }
    
    public async Task<ApiResponse> BuyAsync(Store store, ulong userId)
    {
        var key = $"Buy:{userId}";
        var token = $"{Guid.NewGuid()}";
        
        var bAcquired = await mDistributedLock.TryAcquireLockAsync(key, token);
        
        if (!bAcquired)
            return ApiResponse.From(EResponseResult.LockAcquisitionFailed);
            
        if (store.MaxPurchaseCount != 0)
        {
            var count = await mPurchaseRepository.CountAsync(userId, store.Id);
            
            if (count >= store.MaxPurchaseCount)
                return ApiResponse.From(EResponseResult.PurchaseLimitExceeded);
        }
        
        var purchase = await mPurchaseRepository.AddAndSaveAsync(store.Id, userId);
        
        // TODO 추후 결제 로직 추가 필요

        var bGain = await GainItem(store.Items, userId);
        
        if (!bGain)
            return ApiResponse.From(EResponseResult.ItemCreationFailed);
        
        var bReleased = await mDistributedLock.TryReleaseLockAsync(key, token);
        
        if (!bReleased)
            return ApiResponse.From(EResponseResult.LockReleaseFailed);
        
        await mPurchaseRepository.MarkAsCompletedAsync(purchase.Id);
        
        return ApiResponse.From(EResponseResult.Success);
    }
    
    private async Task<bool> GainItem(Store.Item[] Items, ulong userId)
    {
        GameUser user = null;
        
        foreach (var item in Items)
        {
            if (GameDataManager.ItemTable.TryGet(item.Id, out var value))
            {
                switch (value.ItemKind)
                {
                    case EItemKind.Gold:
                        if (user == null)
                        {
                            user = await mGameUserRepository.FindByUserIdAsync(userId);
                            if (user == null)
                                return false;
                        }

                        if (!user.GainGold(item.Amount))
                            return false;
                        break;
                    case EItemKind.Exp:
                        if (user == null)
                        {
                            user = await mGameUserRepository.FindByUserIdAsync(userId);
                            if (user == null)
                                return false;
                        }

                        if (!user.GainExp(item.Amount))
                            return false;
                        break;
                    case EItemKind.Potion:
                        await mInventoryRepository.AddAsync(item.Id, item.Amount, userId);
                        break;
                }
            }
        }
        return true;
    }
}