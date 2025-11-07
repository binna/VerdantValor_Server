using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SharedLibrary.Efcore;
using SharedLibrary.Redis;
using WebServer.Services;

namespace WebServer.Test.Unit;

public class UsersServiceUnitTest
{
    private readonly UsersService mUsersService;

    public UsersServiceUnitTest(UsersService usersService)
    {
        var logger = Substitute.For<ILogger<UsersService>>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var dbContextFactory = Substitute.For<IDbContextFactory<AppDbContext>>(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("InMemoryDatabase")
                .Options);
        
        // TODO 유저 데이터 넣기
        
        // TODO 우선 임시 방편으로 직접 연결하고, 샘플 하나 만들고 모킹 처리
        var redisClient = Substitute.For<RedisClient>();
        
        mUsersService = new UsersService(logger, httpContextAccessor, dbContextFactory, redisClient);
    }

    [Fact]
    public async Task Join()
    {
        // string email, string pw, string nickname, AppConstant.ELanguage language
        //mUsersService.Join();
    }
}