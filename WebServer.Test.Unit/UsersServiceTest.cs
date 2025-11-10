using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SharedLibrary.Common;
using SharedLibrary.Efcore.Repository;
using SharedLibrary.Models;
using SharedLibrary.Protocol.Common;
using SharedLibrary.Redis;
using WebServer.Services;
using Xunit.Abstractions;

namespace WebServer.Test.Unit;

[Collection("GlobalSetup ResponseStatus")]
public class UsersServiceTest
{
    private readonly ITestOutputHelper mOutput;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly IUsersRepository mUsersRepository;
    private readonly UsersService mUsersService;
    

    public UsersServiceTest(ITestOutputHelper output)
    {
        mOutput = output;
        var logger = Substitute.For<ILogger<UsersService>>();
        mHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
        mUsersRepository = Substitute.For<IUsersRepository>();
        var redisClient = Substitute.For<IRedisClient>();
        mUsersService = Substitute.For<UsersService>(
            logger, mHttpContextAccessor, mUsersRepository, redisClient);
    }

    #region 회원가입
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Test_Join_Email_파라메터가_비었을때_Fail(string? email)
    {
        var response = await mUsersService.Join(email, "1234", "shine");
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.EmptyRequiredField}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Test_Join_Password_파라메터가_비었을때_Fail(string? password)
    {
        var response = await mUsersService.Join("binna", password, "shine");
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.EmptyRequiredField}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Test_Join_Nickname_파라메터가_비었을때_Fail(string? nickname)
    {
        var response = await mUsersService.Join("binna", "1234", nickname);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.EmptyRequiredField}"); 
        Assert.False(response.IsSuccess);
    }

    [Theory]
    [InlineData("돼지꾸르륵")]
    [InlineData("돼지꾸르륵:(")]
    [InlineData("binna:)")]
    [InlineData("binna:)><")]
    public async Task Test_Join_Email_유효하지않는문자_사용할때_Fail(string email)
    {
        var response = await mUsersService.Join(email, "1234", "shine");
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.EmailAlphabetNumberOnly}"); 
        Assert.False(response.IsSuccess);
    }

    [Theory]
    [InlineData("돼지꾸르륵:(")]
    [InlineData("nice:(")]
    public async Task Test_Join_Nickname_유효하지않는문자_사용할때_Fail(string nickname)
    {
        var response = await mUsersService.Join("binna", "1234", nickname);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.NicknameAlphabetKoreanNumberOnly}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Theory]
    [InlineData("binn")]
    [InlineData("bin")]
    [InlineData("bi")]
    [InlineData("b")]
    [InlineData("binnabinnabinnabinnabinnabinnabinnabinnabinnabinna1")]
    [InlineData("binnabinnabinnabinnabinnabinnabinnabinnabinnabinna12")]
    [InlineData("binnabinnabinnabinnabinnabinnabinnabinnabinnabinna123")]
    public async Task Test_Join_Email_길이가_범위_밖일때_Fail(string email)
    {
        var response = await mUsersService.Join(email, "1234", "shine");
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.InvalidEmailLength}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Theory]
    [InlineData("돼")]
    [InlineData("돼지")]
    [InlineData("s")]
    [InlineData("sh")]
    [InlineData("돼지배고파돼지배고파돼지배고파돼지배고파돼지배고파돼지배고파1")]
    [InlineData("돼지배고파돼지배고파돼지배고파돼지배고파돼지배고파돼지배고파12")]
    [InlineData("돼지배고파돼지배고파돼지배고파돼지배고파돼지배고파돼지배고파123")]
    [InlineData("shineshineshineshineshineshine1")]
    [InlineData("shineshineshineshineshineshine12")]
    [InlineData("shineshineshineshineshineshine123")]
    public async Task Test_Join_Nickname_길이가_범위_밖일때_Fail(string nickname)
    {
        var response = await mUsersService.Join("binna", "1234", nickname);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.InvalidNicknameLength}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Fact]
    public async Task Test_Join_이미_가입된_유저일때_Fail()
    {
        mUsersRepository.ExistsUserAsync("binna")
            .Returns(Task.FromResult(true));
        
        var response = await mUsersService.Join("binna", "1234", "shine");
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.EmailAlreadyExists}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Fact]
    public async Task Test_Join_Email_금지된_단어일때_Fail()
    {
        mUsersRepository.ExistsUserAsync("binna")
            .Returns(Task.FromResult(false));
        
        var response = await mUsersService.Join("admin", "1234", "shine");
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.ForbiddenEmail}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Fact]
    public async Task Test_Join_Nickname_금지된_단어일때_Fail()
    {
        mUsersRepository.ExistsUserAsync("binna")
            .Returns(Task.FromResult(false));
        
        var response = await mUsersService.Join("binna", "1234", "admin");
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.ForbiddenNickname}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Fact]
    public async Task Test_Join_Success()
    {
        mUsersRepository.ExistsUserAsync("binna")
            .Returns(Task.FromResult(false));
        
        mUsersRepository.SaveAsync()
            .Returns(Task.FromResult(1));
        
        var response = await mUsersService.Join("binna", "1234", "shine");
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.Success}"); 
        Assert.True(response.IsSuccess);
    }
    #endregion

    #region 로그인
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Test_Login_Email_파라메터가_비었을때_Fail(string? email)
    {
        var user = new Users(
            "binna", "shine", 
            "d404559f602eab6fd602ac7680dacbfaadd13630335e951f097af3900e9de176b6db28512f2e000b9d04fba5133e8b1c6e8df59db3a8ab9d60be4b97cc9e81db");
        
        mUsersRepository.FindUserByEmailAsync(user.Email)
            .Returns(Task.FromResult<Users?>(user));
        
        mHttpContextAccessor.HttpContext.Session.SetString("userId", $"{user.UserId}");
        mHttpContextAccessor.HttpContext.Session.SetString("nickname", $"{user.Nickname}");
        mHttpContextAccessor.HttpContext.Session.SetString("language", $"{AppEnum.ELanguage.En}");
        
        var response = await mUsersService.Login(email, "1234");
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.EmptyRequiredField}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Test_Login_Password_파라메터가_비었을때_Fail(string? password)
    {
        var user = new Users(
            "binna", "shine", 
            "d404559f602eab6fd602ac7680dacbfaadd13630335e951f097af3900e9de176b6db28512f2e000b9d04fba5133e8b1c6e8df59db3a8ab9d60be4b97cc9e81db");
        
        mUsersRepository.FindUserByEmailAsync(user.Email)
            .Returns(Task.FromResult<Users?>(user));
        
        mHttpContextAccessor.HttpContext.Session.SetString("userId", $"{user.UserId}");
        mHttpContextAccessor.HttpContext.Session.SetString("nickname", $"{user.Nickname}");
        mHttpContextAccessor.HttpContext.Session.SetString("language", $"{AppEnum.ELanguage.En}");
        
        var response = await mUsersService.Login("binna", password);
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.EmptyRequiredField}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Theory]
    [InlineData("돼지꾸르륵")]
    [InlineData("돼지꾸르륵:(")]
    [InlineData("binna:)")]
    [InlineData("binna:)><")]
    public async Task Test_Login_Email_유효하지않는문자_사용할때_Fail(string email)
    {
        var user = new Users(
            "binna", "shine", 
            "d404559f602eab6fd602ac7680dacbfaadd13630335e951f097af3900e9de176b6db28512f2e000b9d04fba5133e8b1c6e8df59db3a8ab9d60be4b97cc9e81db");
        
        mUsersRepository.FindUserByEmailAsync(user.Email)
            .Returns(Task.FromResult<Users?>(user));
        
        mHttpContextAccessor.HttpContext.Session.SetString("userId", $"{user.UserId}");
        mHttpContextAccessor.HttpContext.Session.SetString("nickname", $"{user.Nickname}");
        mHttpContextAccessor.HttpContext.Session.SetString("language", $"{AppEnum.ELanguage.En}");
        
        var response = await mUsersService.Join(email, "1234", "shine");
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.EmailAlphabetNumberOnly}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Theory]
    [InlineData("binn")]
    [InlineData("bin")]
    [InlineData("bi")]
    [InlineData("b")]
    [InlineData("binnabinnabinnabinnabinnabinnabinnabinnabinnabinna1")]
    [InlineData("binnabinnabinnabinnabinnabinnabinnabinnabinnabinna12")]
    [InlineData("binnabinnabinnabinnabinnabinnabinnabinnabinnabinna123")]
    public async Task Test_Login_Email_길이가_범위_밖일때_Fail(string email)
    {
        var user = new Users(
            "binna", "shine", 
            "d404559f602eab6fd602ac7680dacbfaadd13630335e951f097af3900e9de176b6db28512f2e000b9d04fba5133e8b1c6e8df59db3a8ab9d60be4b97cc9e81db");
        
        mUsersRepository.FindUserByEmailAsync(user.Email)
            .Returns(Task.FromResult<Users?>(user));
        
        mHttpContextAccessor.HttpContext.Session.SetString("userId", $"{user.UserId}");
        mHttpContextAccessor.HttpContext.Session.SetString("nickname", $"{user.Nickname}");
        mHttpContextAccessor.HttpContext.Session.SetString("language", $"{AppEnum.ELanguage.En}");
        
        var response = await mUsersService.Login(email, "1234");
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.InvalidEmailLength}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Fact]
    public async Task Test_Login_Email_가입된_유저를_찾을수없을때_Fail()
    {
        var user = new Users(
            "binna", "shine", 
            "d404559f602eab6fd602ac7680dacbfaadd13630335e951f097af3900e9de176b6db28512f2e000b9d04fba5133e8b1c6e8df59db3a8ab9d60be4b97cc9e81db");
        
        mUsersRepository.FindUserByEmailAsync(user.Email)
            .Returns(Task.FromResult<Users?>(user));
        
        mHttpContextAccessor.HttpContext.Session.SetString("userId", $"{user.UserId}");
        mHttpContextAccessor.HttpContext.Session.SetString("nickname", $"{user.Nickname}");
        mHttpContextAccessor.HttpContext.Session.SetString("language", $"{AppEnum.ELanguage.En}");
        
        var response = await mUsersService.Login("shine94", "1234");
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.NoData}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Fact]
    public async Task Test_Login_Success()
    {
        var user = new Users(
            "binna",
            "shine", 
            "d404559f602eab6fd602ac7680dacbfaadd13630335e951f097af3900e9de176b6db28512f2e000b9d04fba5133e8b1c6e8df59db3a8ab9d60be4b97cc9e81db");
        
        mUsersRepository.FindUserByEmailAsync(user.Email)
            .Returns(Task.FromResult<Users?>(user));
        
        mHttpContextAccessor.HttpContext.Session.SetString("userId", $"{user.UserId}");
        mHttpContextAccessor.HttpContext.Session.SetString("nickname", $"{user.Nickname}");
        mHttpContextAccessor.HttpContext.Session.SetString("language", $"{AppEnum.ELanguage.En}");
        
        var response = await mUsersService.Login("binna", "1234");
        Assert.Equal($"{response.Code}", $"{(int)EResponseStatus.Success}"); 
        Assert.True(response.IsSuccess);
    }
    #endregion
}