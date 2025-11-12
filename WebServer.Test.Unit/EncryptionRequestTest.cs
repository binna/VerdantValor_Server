using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SharedLibrary.Common;
using SharedLibrary.Efcore.Repository;
using SharedLibrary.Efcore.Transaction;
using SharedLibrary.Models;
using SharedLibrary.Protocol.Common;
using SharedLibrary.Protocol.DTOs;
using SharedLibrary.Redis;
using StackExchange.Redis;
using WebServer.Controllers;
using WebServer.Services;
using Xunit.Abstractions;

namespace WebServer.Test.Unit;

[Collection("GlobalSetup ResponseStatus")]
public class EncryptionRequestTest
{
    private readonly ITestOutputHelper mOutput;
    private readonly byte[] key;
    
    public EncryptionRequestTest(ITestOutputHelper output)
    {
        mOutput = output;
        key = Encoding.UTF8.GetBytes("ABCDEFGHIJKLMNOP");
    }

#if LIVE
    #region 스웨거에서 테스트하기 위한 직접 코드 암호화 출력
    //[Fact]
    public void ShowEncryptReq()
    {
        var req = new GetRankReq
        {
            Scope = $"{AppEnum.ERankingScope.Global}",
            Type = $"{AppEnum.ERankingType.All}",
            Limit = 100
        };

        var result = EncryptReq(req);
        mOutput.WriteLine(result.Nonce);
        mOutput.WriteLine(result.Tag);
        mOutput.WriteLine(result.Data);
    }
    
    private EncryptReq EncryptReq<T>(T req)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
    
        var plainText = JsonSerializer.Serialize(req);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16];
    
        using var aesCcm = new AesCcm(key);
        aesCcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        
        return new EncryptReq
        {
            Nonce = Convert.ToBase64String(nonce),
            Tag = Convert.ToBase64String(tag),
            Data = Convert.ToBase64String(cipherBytes)
        };
    }
    #endregion
#endif

    #region UsersController
    [Fact]
    public async Task Test_Auth_회원가입할때_Success()
    {
        var logger = Substitute.For<ILogger<UsersService>>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var usersRepository = Substitute.For<IUsersRepository>();
        var usersTransaction = Substitute.For<IUsersServiceTransaction>();
        var redisClient = Substitute.For<IRedisClient>();
        var usersService = Substitute.For<UsersService>(
            logger, httpContextAccessor, usersRepository, usersTransaction, redisClient);
        var usersController = Substitute.For<UsersController>(usersService);
        
        var req = new AuthReq
        {
            Email = "binna",
            Nickname = "shine",
            Pw = "1234",
            AuthType = $"{AppEnum.EAuthType.Join}",
            Language = $"{AppEnum.ELanguage.Ko}"
        };
        
        usersTransaction.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .ReturnsForAnyArgs(Task.FromResult(1));
        
#if DEVELOPMENT
        var response = await usersController.Auth(req);
#elif LIVE
        var response = await usersController.Auth(EncryptReq(req));
#endif
        
        Assert.True(response.IsSuccess);
        Assert.Equal((int)EResponseStatus.Success, response.Code);
    }
    
    [Fact]
    public async Task Test_Auth_로그인할때_Success()
    {
        var logger = Substitute.For<ILogger<UsersService>>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var usersRepository = Substitute.For<IUsersRepository>();
        var usersTransaction = Substitute.For<IUsersServiceTransaction>();
        var redisClient = Substitute.For<IRedisClient>();
        var usersService = Substitute.For<UsersService>(
            logger, httpContextAccessor, usersRepository, usersTransaction, redisClient);
        var usersController = Substitute.For<UsersController>(usersService);
        
        var user = new Users(
            "binna", "shine", 
            "d404559f602eab6fd602ac7680dacbfaadd13630335e951f097af3900e9de176b6db28512f2e000b9d04fba5133e8b1c6e8df59db3a8ab9d60be4b97cc9e81db");
        
        var req = new AuthReq
        {
            Email = "binna",
            Pw = "1234",
            AuthType = $"{AppEnum.EAuthType.Login}",
            Language = $"{AppEnum.ELanguage.Ko}"
        };
        
        usersRepository.FindUserByEmailAsync(Arg.Any<string>())
            .Returns(Task.FromResult<Users?>(user));
        
#if DEVELOPMENT
        var response = await usersController.Auth(req);
#elif LIVE
        var response = await usersController.Auth(EncryptReq(req));
#endif
        
        Assert.True(response.IsSuccess);
        Assert.Equal((int)EResponseStatus.Success, response.Code);
    }
    #endregion

    #region RankingController
    [Fact]
    public async Task Test_GetRank_랭크_조회할때_Success()
    {
        var logger = Substitute.For<ILogger<RankingService>>();
        var redisClient = Substitute.For<IRedisClient>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var rankingService = Substitute.For<RankingService>(
            logger, redisClient);
        var rankingController = Substitute.For<RankingController>(
            rankingService, httpContextAccessor, redisClient);
        
        var req = new GetRankReq
        {
            Scope = $"{AppEnum.ERankingScope.Global}",
            Type = $"{AppEnum.ERankingType.All}",
            Limit = 100
        };
        
        var http = new DefaultHttpContext();
        httpContextAccessor.HttpContext.Returns(http);
        
        var session = Substitute.For<ISession>();
        
        session.TryGetValue(Arg.Is("userId"), out Arg.Any<byte[]>())
            .Returns(ci => { ci[1] = Encoding.UTF8.GetBytes("1"); return true; });
        
        session.TryGetValue(Arg.Is("nickname"), out Arg.Any<byte[]>())
            .Returns(ci => { ci[1] = Encoding.UTF8.GetBytes("1"); return true; });
        
        http.Session = session;

        redisClient.GetSessionInfoAsync(Arg.Any<string>())
            .Returns(Task.FromResult<RedisValue>(""));
        
        redisClient.GetTopRankingByType(Arg.Any<string>(), Arg.Any<int>())
            .Returns(Task.FromResult(
                new []
                {
                    new SortedSetEntry("1/user1", 1000.0),
                    new SortedSetEntry("2/user2", 900.0),
                    new SortedSetEntry("3/user3", 800.0),
                    new SortedSetEntry("4/user4", 700.0),
                    new SortedSetEntry("5/user5", 600.0),
                }
            ));
#if DEVELOPMENT
        var response = await rankingController.GetRank(req);
#elif LIVE
        var response = await rankingController.GetRank(EncryptReq(req));
#endif
        
        Assert.True(response.IsSuccess);
        Assert.Equal((int)EResponseStatus.Success, response.Code);
    }
    
    [Fact]
    public async Task Test_GetRank_랭크점수_추가할때_Success()
    {
        var logger = Substitute.For<ILogger<RankingService>>();
        var redisClient = Substitute.For<IRedisClient>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var rankingService = Substitute.For<RankingService>(
            logger, redisClient);
        var rankingController = Substitute.For<RankingController>(
            rankingService, httpContextAccessor, redisClient);
        
        var req = new CreateScoreReq
        {
            Type = $"{AppEnum.ERankingType.All}",
            Score = 1000
        };
        
        var http = new DefaultHttpContext();
        httpContextAccessor.HttpContext.Returns(http);
        
        var session = Substitute.For<ISession>();
        
        session.TryGetValue(Arg.Is("userId"), out Arg.Any<byte[]>())
            .Returns(ci => { ci[1] = Encoding.UTF8.GetBytes("1"); return true; });
        
        session.TryGetValue(Arg.Is("nickname"), out Arg.Any<byte[]>())
            .Returns(ci => { ci[1] = Encoding.UTF8.GetBytes("1"); return true; });
        
        http.Session = session;

        redisClient.GetSessionInfoAsync(Arg.Any<string>())
            .Returns(Task.FromResult<RedisValue>(""));
        
        redisClient.GetTopRankingByType(Arg.Any<string>(), Arg.Any<int>())
            .Returns(Task.FromResult(
                new []
                {
                    new SortedSetEntry("1/user1", 1000.0),
                    new SortedSetEntry("2/user2", 900.0),
                    new SortedSetEntry("3/user3", 800.0),
                    new SortedSetEntry("4/user4", 700.0),
                    new SortedSetEntry("5/user5", 600.0),
                }
            ));
#if DEVELOPMENT
        var response = await rankingController.Entries(req);
#elif LIVE
        var response = await rankingController.Entries(EncryptReq(req));
#endif
        
        Assert.True(response.IsSuccess);
        Assert.Equal((int)EResponseStatus.Success, response.Code);
    }
    #endregion
}