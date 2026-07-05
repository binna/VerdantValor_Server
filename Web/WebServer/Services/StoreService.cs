using Common.Concurrency;
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
    private readonly IDistributedLock mDistributedLock;
    private readonly ItemService mItemService;
    
    public StoreService(
        ILogger<StoreService> logger,
        IPurchaseRepository purchaseRepository,
        IGameUserRepository gameUserRepository,
        IDistributedLock distributedLock,
        ItemService itemService)
    {
        mLogger = logger;
        mPurchaseRepository = purchaseRepository;
        mGameUserRepository = gameUserRepository;
        mDistributedLock = distributedLock;
        mItemService = itemService;
    }
    
    // TODO 문제가 생기면 TTL에 의존하지 말고 락을 해제할 수 있는 방법을 찾자
    //  1. Try - Catch를 이용하기
    //  2. using을 이용해보기
    public async Task<ApiResponse> BuyAsync(Store store, ulong userId)
    {
        var key = $"Buy:{userId}";
        var token = $"{Guid.NewGuid()}";
        
        var bAcquired = await mDistributedLock.TryAcquireLockAsync(key, token);

        if (!bAcquired)
            return ApiResponse.From(EResponseResult.LockAcquisitionFailed);
        
        Purchase purchase;

        try
        {
            if (store.MaxPurchaseCount != 0)
            {
                var count = await mPurchaseRepository.CountAsync(userId, store.Id);

                if (count >= store.MaxPurchaseCount)
                    return ApiResponse.From(EResponseResult.PurchaseLimitExceeded);
            }

            purchase = await mPurchaseRepository.AddAndSaveAsync(store.Id, userId);

            var user = await mGameUserRepository.FindByUserIdAsync(userId);

            if (user == null)
                return ApiResponse.From(EResponseResult.NoData);

            var bPurchaseSuccess = Purchase(store.PriceType, store.Prices, user);

            if (!bPurchaseSuccess)
                return ApiResponse.From(EResponseResult.PurchaseFailed);

            var bItemGranted = await mItemService.GainItem(store.Items, user);

            if (!bItemGranted)
                return ApiResponse.From(EResponseResult.ItemCreationFailed);
        }
        finally
        {
            var bReleased = await mDistributedLock.TryReleaseLockAsync(key, token);

            if (!bReleased)
            {
                // TODO 알람/모니터링 연동 고려
                //  다시 생각해보니 결국, 락 해제만 실패했을 뿐, 결제는 성공함
                //  우선 이 부분은 좀 더 고민해보기
                mLogger.LogWarning($"Lock release failed: key={key}, token={token}");
            }
        }
        
        await mPurchaseRepository.MarkAsCompletedAsync(purchase.Id);
        
        return ApiResponse.From(EResponseResult.Success);
    }

    private static bool Purchase(ECurrencyType priceType, Dictionary<ECurrency, decimal> prices, GameUser user)
    {
        if (priceType == ECurrencyType.Game)
        {
            if (user.Gold < prices[ECurrency.Gold])
                return false;
            
            return user.UseGold((int)prices[ECurrency.Gold]);;
        }
        
        // TODO 유료 결제
        Console.WriteLine("유료 결제");
        return true;
    }
}