using Common.Concurrency;
using Common.Driver;
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

    public StoreServiceTest(ITestOutputHelper output)
    {
        mOutput = output;
        mPurchaseRepository = Substitute.For<IPurchaseRepository>();
        mGameUserRepository = Substitute.For<IGameUserRepository>();
        mInventoryRepository = Substitute.For<IInventoryRepository>();
        mDistributedLock = Substitute.For<IDistributedLock>();

        mStoreService = Substitute.For<StoreService>(
            Substitute.For<ILogger<StoreService>>(),
            mPurchaseRepository, 
            mGameUserRepository, 
            mInventoryRepository,
            mDistributedLock);
    }

    [Fact]
    public async Task Test_락_획득_실패시_Fail()
    {
        mDistributedLock.TryAcquireLockAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));
        
        var store = new Store
        {
            Id = 1,
            Items = [],
            Prices = [],
            TextId = 37,
            MaxPurchaseCount = 1
        };
        
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
        
        var store = new Store
        {
            Id = 1,
            Items = [],
            Prices = [],
            TextId = 37,
            MaxPurchaseCount = 1
        };
        
        var response = await mStoreService.BuyAsync(store, 1);
        Assert.Equal($"{(int)EResponseResult.PurchaseLimitExceeded}", $"{response.Code}");
    }
    
    [Fact]
    public async Task Test_GainItem_실패시_Fail()
    {
        mDistributedLock.TryAcquireLockAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        
        mPurchaseRepository.CountAsync(Arg.Any<ulong>(), Arg.Any<int>())
            .Returns(Task.FromResult(0));

        // TODO 이 부분을 Service를 분리해서 virtual로 만들 예정
        mGameUserRepository.FindByUserIdAsync(Arg.Any<ulong>())
            .Returns(Task.FromResult<GameUser?>(null));
        
        var store = new Store
        {
            Id = 1,
            Items = [new Store.Item { Id = 9, Amount = 1 }],
            Prices = [],
            TextId = 37,
            MaxPurchaseCount = 1
        };
        
        var response = await mStoreService.BuyAsync(store, 1);
        Assert.Equal($"{(int)EResponseResult.ItemCreationFailed}", $"{response.Code}");
    }
    
    
    [Fact]
    public async Task Test_락_반환_실패시_Fail()
    {
        mDistributedLock.TryAcquireLockAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));
        
        mPurchaseRepository.CountAsync(Arg.Any<ulong>(), Arg.Any<int>())
            .Returns(Task.FromResult(0));
        
        mDistributedLock.TryReleaseLockAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var store = new Store
        {
            Id = 1,
            Items = [],
            Prices = [],
            TextId = 37,
            MaxPurchaseCount = 1
        };
        
        var response = await mStoreService.BuyAsync(store, 1);
        Assert.Equal($"{(int)EResponseResult.LockReleaseFailed}", $"{response.Code}");
    }
}