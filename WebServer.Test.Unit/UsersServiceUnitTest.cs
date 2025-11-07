using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SharedLibrary.Common;
using SharedLibrary.Efcore;
using SharedLibrary.Models;
using SharedLibrary.Protocol.Common;
using SharedLibrary.Redis;
using WebServer.Common;
using WebServer.Helpers;
using WebServer.Services;
using Xunit.Abstractions;

namespace WebServer.Test.Unit;

public class UsersServiceUnitTest
{
    private readonly ITestOutputHelper mOutput;
    private readonly UsersService mUsersService;
    private readonly InMemoryDatabaseRoot mInMemoryDatabaseRoot = new();
    private readonly IDbContextFactory<AppDbContext> mDbContextFactory;

    public UsersServiceUnitTest(ITestOutputHelper output)
    {
        mOutput = output;
        
        var logger = Substitute.For<ILogger<UsersService>>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();

        #region DB InMemory 설정과 데이터 미리 세팅
        var option = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("TestDb", mInMemoryDatabaseRoot)
            .Options;

        using (var context = new AppDbContext(option))
        {
            context.Users.AddRange(
                new Users("binna", "binna", HashHelper.ComputeSha512Hash("1234")),
                new Users("강아지멍멍", "binna94", HashHelper.ComputeSha512Hash("1234"))
            );
            
            var rows = context.SaveChanges();
            mOutput.WriteLine("rows: " + rows);
        }
        
        var dbContextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        
        dbContextFactory.CreateDbContext()
            .Returns(_ => new AppDbContext(option));
        
        dbContextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new AppDbContext(option)));
        
        mDbContextFactory = dbContextFactory;
        #endregion
        
        IRedisClient redisClient = 
            Substitute.For<FakeRedisClient>("localhost", "6379", 5, 6);

        var baseDir = AppContext.BaseDirectory;
        var path = Path.Combine(
            baseDir, 
            AppConstant.SHARED_LIBRARY_PATH, 
            "GameData", "Data", "ResponseStatus.json");
        
        ResponseStatus.Init(path);

        mUsersService = new UsersService(
            logger, httpContextAccessor, dbContextFactory, redisClient);
    }

    [Fact]
    public async Task Join()
    {
        ApiResponse response;
        string email;
        string nickname;
        string password;
        var language = AppEnum.ELanguage.Ko;
        
        //  1. 이메일은 영어 숫자
        email = "돼지꾸륵";
        nickname = "돼지배고파";
        password = "1234";
        response = await mUsersService.Join(email, password, nickname, language);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.EmailAlphabetNumberOnly}");
        Assert.False(response.IsSuccess);
        
        email = "#$@";
        nickname = "돼지배고파";
        password = "1234";
        response = await mUsersService.Join(email, password, nickname, language);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.EmailAlphabetNumberOnly}");
        Assert.False(response.IsSuccess);

        //  2. 닉네임은 영어 숫자 한글
        email = "shine94";
        nickname = "_@돼지배고파";
        password = "1234";
        response = await mUsersService.Join(email, password, nickname, language);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.NicknameAlphabetKoreanNumberOnly}");
        Assert.False(response.IsSuccess);
        
        //  2. 이메일과 닉네임 길이
        email = "shin";
        nickname = "돼지배고파";
        password = "1234";
        response = await mUsersService.Join(email, password, nickname, language);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.InvalidEmailLength}");
        Assert.False(response.IsSuccess);
        
        email = "shineshineshineshineshineshineshineshineshineshine1";
        nickname = "돼지배고파";
        password = "1234";
        response = await mUsersService.Join(email, password, nickname, language);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.InvalidEmailLength}");
        Assert.False(response.IsSuccess);
        
        email = "shine94";
        nickname = "돼지";
        password = "1234";
        response = await mUsersService.Join(email, password, nickname, language);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.InvalidNicknameLength}");
        Assert.False(response.IsSuccess);
        
        email = "shine94";
        nickname = "돼지배고파돼지배고파돼지배고파돼지배고파돼지배고파돼지배고파1";
        password = "1234";
        response = await mUsersService.Join(email, password, nickname, language);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.InvalidNicknameLength}");
        Assert.False(response.IsSuccess);
        
        await using var db = await mDbContextFactory.CreateDbContextAsync();
        int count = await db.Users.CountAsync();
        mOutput.WriteLine("Count: " + count);
                
        //  3. 이미 가입된 유저
        email = "binna94";
        nickname = "돼지배고파";
        password = "1234";
        response = await mUsersService.Join(email, password, nickname, language);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.EmailAlreadyExists}");
        Assert.False(response.IsSuccess);
        
        //  5. 금지된 email과 닉네임
        email = "admin";
        nickname = "돼지배고파";
        password = "1234";
        response = await mUsersService.Join(email, password, nickname, language);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.ForbiddenEmail}");
        Assert.False(response.IsSuccess);
        
        email = "shine94";
        nickname = "admin";
        password = "1234";
        response = await mUsersService.Join(email, password, nickname, language);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.ForbiddenNickname}");
        Assert.False(response.IsSuccess);
        
        //  6. 최종적으로 가입됬는지 확인하기
        email = "shine94";
        nickname = "돼지배고파";
        password = "1234";
        response = await mUsersService.Join(email, password, nickname, language);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.Success}");
        Assert.True(response.IsSuccess);
        
        //  7. 디비에 비밀번호 그대로 저장 금지
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email);
        Assert.NotNull(user);
        mOutput.WriteLine("saved pw : " + user.Pw);
        mOutput.WriteLine("plain pw : " + password);
        Assert.NotEqual($"{user.Pw}", $"{password}");
    }
}