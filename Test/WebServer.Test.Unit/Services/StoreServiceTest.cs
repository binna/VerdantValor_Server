using Common.Concurrency;
using Common.Driver;
using Common.Manager;
using Common.Models;
using Common.Web;
using Efcore.Repositories;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.GameData;
using Shared.Types;
using WebServer.Services;
using Xunit.Abstractions;

namespace WebServer.Test.Unit.Services;

[Collection("GlobalSetup ResponseStatus")]
public class StoreServiceTest
{
    private readonly ITestOutputHelper mOutput;
    private readonly IKeyValueStore mKeyValueStore;
    private readonly IPurchaseRepository mPurchaseRepository;
    private readonly IGameUserRepository mGameUserRepository;
    private readonly IInventoryRepository mInventoryRepository;
    private readonly ICacheDriver mCacheDriver;
    private readonly IDistributedLock mDistributedLock;
    private readonly StoreService mStoreService;
    private readonly ItemService mItemService;

    public StoreServiceTest(ITestOutputHelper output)
    {
        mOutput = output;
        mPurchaseRepository = Substitute.For<IPurchaseRepository>();
        mGameUserRepository = Substitute.For<IGameUserRepository>();
        mInventoryRepository = Substitute.For<IInventoryRepository>();
        mDistributedLock = Substitute.For<IDistributedLock>();
        mItemService = Substitute.For<ItemService>(
            Substitute.For<ILogger<ItemService>>(),
            mInventoryRepository);

        mStoreService = Substitute.For<StoreService>(
            Substitute.For<ILogger<StoreService>>(),
            mPurchaseRepository, 
            mGameUserRepository, 
            mDistributedLock, 
            mItemService);
    }

    [Fact]
    public void Test_Store_Get_유효한Id_정상데이터반환()
    {
        var result = GameDataManager.StoreTable.TryGet(1, out var store);
        
        Assert.True(result);
        Assert.NotNull(store);
        Assert.Equal(1, store.Id);
        Assert.Equal(0, store.Prices[ECurrency.Gold]);
    }

    [Fact]
    public async Task Test_락_획득_실패시_Fail()
    {
        mDistributedLock.TryAcquireLockAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));
        
        GameDataManager.StoreTable.TryGet(1, out var store);
        
        var response = await mStoreService.BuyAsync(store, 1);
        Assert.Equal($"{(int)EResponseResult.LockAcquisitionFailed}", $"{response.Code}");
    }

    [Fact]
    public async Task Test_구매_제한_초과시_Fail()
    {
        mDistributedLock.TryAcquireLockAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        
        mPurchaseRepository.CountAsync(Arg.Any<ulong>(), Arg.Any<int>())
            .Returns(Task.FromResult(1));
        
        GameDataManager.StoreTable.TryGet(1, out var store);
        
        var response = await mStoreService.BuyAsync(store, 2);
        Assert.Equal($"{(int)EResponseResult.PurchaseLimitExceeded}", $"{response.Code}");
    }
    
    [Fact]
    public async Task Test_GainItem_실패시_Fail()
    {
        mDistributedLock.TryAcquireLockAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        
        mPurchaseRepository.CountAsync(Arg.Any<ulong>(), Arg.Any<int>())
            .Returns(Task.FromResult(0));

        mGameUserRepository.FindByUserIdAsync(Arg.Any<ulong>())
            .Returns(Task.FromResult<GameUser?>(new GameUser("shine", "shine", "1234")));
        
        var store = new Store
        {
            Id = 1,
            Items = [new Store.Item { Id = 9, Amount = -5 }],
            Prices = new Dictionary<ECurrency, decimal> {[ECurrency.Gold] = 0},
            TextId = 37,
            PriceType = ECurrencyType.Game,
            MaxPurchaseCount = 1
        };
        
        var response = await mStoreService.BuyAsync(store, 3);
        Assert.Equal($"{(int)EResponseResult.ItemCreationFailed}", $"{response.Code}");
    }
    
    [Fact]
    public async Task Test_결제_실패시_Fail()
    {
        mDistributedLock.TryAcquireLockAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        
        mPurchaseRepository.CountAsync(Arg.Any<ulong>(), Arg.Any<int>())
            .Returns(Task.FromResult(0));
        
        mGameUserRepository.FindByUserIdAsync(Arg.Any<ulong>())
            .Returns(Task.FromResult<GameUser?>(new GameUser("shine", "shine", "1234")));
        
        mDistributedLock.TryReleaseLockAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var store = new Store
        {
            Id = 1,
            Items = [new Store.Item { Id = 3, Amount = 1 }],
            Prices = new Dictionary<ECurrency, decimal> {[ECurrency.Gold] = 100},
            TextId = 37,
            MaxPurchaseCount = 1
        };
        
        var response = await mStoreService.BuyAsync(store, 4);
        Assert.Equal($"{(int)EResponseResult.PurchaseFailed}", $"{response.Code}");
    }
    
    [Fact]
    public async Task Test_락_반환_실패시_Fail()
    {
        mDistributedLock.TryAcquireLockAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        
        mPurchaseRepository.CountAsync(Arg.Any<ulong>(), Arg.Any<int>())
            .Returns(Task.FromResult(0));
        
        mGameUserRepository.FindByUserIdAsync(Arg.Any<ulong>())
            .Returns(Task.FromResult<GameUser?>(new GameUser("shine", "shine", "1234")));
        
        mDistributedLock.TryReleaseLockAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        GameDataManager.StoreTable.TryGet(1, out var store);
        
        var response = await mStoreService.BuyAsync(store, 5);
        Assert.Equal($"{(int)EResponseResult.LockReleaseFailed}", $"{response.Code}");
    }
}