using Common;
using Common.Concurrency;
using Common.Driver;
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
    private readonly ICacheDriver mDistributedLockDriver;
    private readonly ItemService mItemService;
    
    public StoreService(
        ILogger<StoreService> logger,
        IPurchaseRepository purchaseRepository,
        IGameUserRepository gameUserRepository,
        ICacheDriver distributedLockDriver,
        ItemService itemService)
    {
        mLogger = logger;
        mPurchaseRepository = purchaseRepository;
        mGameUserRepository = gameUserRepository;
        mDistributedLockDriver = distributedLockDriver;
        mItemService = itemService;
    }
    
    // 문제가 생기면 TTL에 의존하지 말고 락을 해제할 수 있는 방법을 찾자
    //  1. Try ~ Finally를 이용하기
    //  2. using을 이용해보기
    ///////////////////////////////////////////////////////////////
    // 컨트롤러 (요청마다 새로 생성)
    //     │ 생성자 주입 (DI)
    //     ▼
    // ICacheDriver (싱글톤, 앱 전체에서 하나만 존재)
    //     │
    //     ▼
    // DistributedLockHandle (매번 새로 생성되는 인스턴스)
    ///////////////////////////////////////////////////////////////
    // 왜 이런 구조가 필요했나?
    // 처음에 사용한 Try - Finally로 처리했다.
    // 이 경우 key와 token은 그냥 지역 변수로 finally에서 바로 참조하면 되니,
    // 별도로 넘겨줄 고민이 필요 없었다.
    //
    // 다만 이 구조를 여러 메서드에서 반복해서 써야 한다면,
    // 매번 try-finally를 쓰는 대신 재사용 가능한 형태로
    // 만들 수는 없을지 고민했다.
    // 사용하면서 휴먼 에러도 발생할 수도 있고
    //
    // using과 Dispose로 처리하려면
    // key와 token을 handle 생성 시점에 넘겨서 필드로 들고 있어야 하는데
    // 이때, key와 token을 싱글톤에 저장하면 여러 API 콜이 동시에 
    // 들어올 때 서로 값을 덮어쓰는 문제가 생긴다.
    //
    // 그래서 새로 생성되는 별도 객체(handle)에 두는 방식을 고민했고,
    // 윈도우의 File Handle이 대표적인 예시다.
    //
    // 다만 지금처럼 한 곳에서만 쓰는 경우엔
    // try-finally만으로 충분하다고 생각한다.
    // 그럼에도 학습 목적이 크고,
    // 앞으로 락 사용처가 늘어나면
    // 이 구조가 더 유용해질 거라 판단해 이번엔 적용해본다.
    public async Task<ApiResponse> BuyAsync(Store store, ulong userId)
    {
        var key = $"Buy:{userId}";
        var token = $"{Guid.NewGuid()}";

        Purchase purchase;

        {
            await using var distributedLockHandle = 
                new DistributedLockHandle(mDistributedLockDriver, key, token, ShareServerConst.LOCK_EXPIRY_MS);

            var bAcquired = await distributedLockHandle.TryAcquireGlobalLockAsync();
            if (!bAcquired)
                return ApiResponse.From(EResponseResult.LockAcquisitionFailed);
            
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