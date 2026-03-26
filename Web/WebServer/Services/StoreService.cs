using Common.Concurrency;
using Common.Manager;
using Common.Models;
using Common.Web;
using Efcore.Repositories;
using Redis;
using Shared.GameData;
using Shared.Types;

namespace WebServer.Services;

public class StoreService
{
    private readonly ILogger<StoreService> mLogger;
    private readonly IPurchaseRepository mPurchaseRepository;
    private readonly IGameUserRepository mGameUserRepository;
    private readonly DistributedLock mDistributedLock;
    
    public StoreService(
        ILogger<StoreService> logger,
        IPurchaseRepository purchaseRepository,
        IGameUserRepository gameUserRepository)
    {
        mLogger = logger;
        mPurchaseRepository = purchaseRepository; 
        mGameUserRepository = gameUserRepository;
        
        var cacheDriver = new RedisCacheDriver("localhost", "6379", 10);
        mDistributedLock = new DistributedLock(cacheDriver, 1000);
    }
    
    public async Task<ApiResponse> BuyAsync(Store store, ulong userId)
    {
        var key = $"Buy:{userId}";
        var token = $"{Guid.NewGuid()}";
        
        Console.WriteLine(key);
        
        var bAcquired =  await mDistributedLock.TryAcquireLockAsync(key, token);
        
        if (!bAcquired)
            return ApiResponse.From(EResponseResult.LockAcquisitionFailed);
            
        if (store.MaxPurchaseCount != 0)
        {
            var count = await mPurchaseRepository.CountAsync(userId, store.Id);
            
            if (count > store.MaxPurchaseCount)
                return ApiResponse.From(EResponseResult.PurchaseLimitExceeded);
        }
        
        var purchase = await mPurchaseRepository.AddAsync(store.Id, userId);

        var bGain = await GainItem(store.Items, userId);
        
        if (!bGain)
            return ApiResponse.From(EResponseResult.ItemCreationFailed);
        
        var bReleased = await mDistributedLock.TryReleaseLockAsync(key, token);
        
        if (!bReleased)
            return ApiResponse.From(EResponseResult.LockReleaseFailed);
        
        await mPurchaseRepository.MarkAsCompletedAsync(purchase);
        return ApiResponse.From(EResponseResult.Success);
    }
    
    private async Task<bool> GainItem(Store.Item[] Items, ulong userId)
    {
        GameUser user = null;
        
        foreach (var item in Items)
        {
            Console.WriteLine(item.Id);
            Console.WriteLine(item.Amount);

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
                        // TODO 인벤토리 테이블 만들 필요 있음
                        break;
                }
            }
        }
        return true;
    }
}