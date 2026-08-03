using Common.Helpers;
using Common.KeyValueStore;
using Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Efcore.Repositories;
using Shared.Types;
using WebServer.options;
using WebServer.Services;
using Xunit.Abstractions;

namespace WebServer.Test.Unit.Services;

[Collection("GlobalSetup ResponseStatus")]
public class GameUserServiceTest
{
    private readonly ITestOutputHelper mOutput;
    private readonly GameUserService mGameUserService;
    private readonly IGameUserRepository mGameUserRepository;
    private readonly ISessionKeyValueStore mSessionKeyValueStore;
    private readonly ISecurityHelper mSecurityHelper;
    private readonly ServerOption mServerOption;

    public GameUserServiceTest(ITestOutputHelper output)
    {
        mOutput = output;
        mGameUserRepository = Substitute.For<IGameUserRepository>();
        mSessionKeyValueStore = Substitute.For<ISessionKeyValueStore>();
        mSecurityHelper = Substitute.For<ISecurityHelper>();
        mServerOption = Substitute.For<ServerOption>();
        mGameUserService = Substitute.For<GameUserService>(
            Substitute.For<ILogger<GameUserService>>(), 
            Substitute.For<IHttpContextAccessor>(), 
            mGameUserRepository,
            mSessionKeyValueStore,
            mSecurityHelper,
            mServerOption);
    }

    #region 회원가입
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Test_Join_Email_파라미터가_비었을때_Fail(string? email)
    {
        var code = await mGameUserService.JoinAsync(email, "Alpha123^", "shine");
        
        Assert.Equal($"{EResponseResult.EmptyRequiredField}", $"{code}"); 
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Test_Join_Password_파라미터가_비었을때_Fail(string? password)
    {
        var code = await mGameUserService.JoinAsync("every5116@naver.com", password, "shine");
        
        Assert.Equal($"{EResponseResult.EmptyRequiredField}", $"{code}"); 
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Test_Join_Nickname_파라미터가_비었을때_Fail(string? nickname)
    {
        var code = await mGameUserService.JoinAsync("every5116@naver.com", "Alpha123^", nickname);
        
        Assert.Equal($"{EResponseResult.EmptyRequiredField}", $"{code}"); 
    }
    
    [Theory]
    [InlineData("돼지꾸르륵@naver.com")]
    [InlineData("돼지꾸르륵:(@naver.com")]
    [InlineData("binna:)@naver.com")]
    [InlineData("binna:)><@naver.com")]
    public async Task Test_Join_Email_유효하지않는문자_사용할때_Fail(string email)
    {
        var code = await mGameUserService.JoinAsync(email, "Alpha123^", "shine");
        
        Assert.Equal($"{EResponseResult.InvalidEmailFormat}", $"{code}"); 
    }
    
    [Theory]
    [InlineData("159753456")]
    [InlineData("alphahappy")]
    [InlineData("Alphahappy")]
    [InlineData("Alphahappy123")]
    public async Task Test_Join_Password_유효하지않는문자_사용할때_Fail(string password)
    {
        var code = await mGameUserService.JoinAsync("every5116@naver.com", password, "shine");
        
        Assert.Equal($"{EResponseResult.InvalidPasswordFormat}", $"{code}"); 
    }
    
    [Theory]
    [InlineData("돼지꾸르륵:(")]
    [InlineData("nice:(")]
    [InlineData("nice/")]
    public async Task Test_Join_Nickname_유효하지않는문자_사용할때_Fail(string nickname)
    {
        var code = await mGameUserService.JoinAsync("every5116@naver.com", "Alpha123^", nickname);
        
        Assert.Equal($"{EResponseResult.InvalidNicknameFormat}", $"{code}"); 
    }
    
    [Theory]
    [InlineData("binn@naver.com")]
    [InlineData("bin@naver.com")]
    [InlineData("bi@naver.com")]
    [InlineData("b@naver.com")]
    [InlineData("binnabinnabinnabinnabinnabinnabinnabinnabinnabinna1@naver.com")]
    [InlineData("binnabinnabinnabinnabinnabinnabinnabinnabinnabinna12@naver.com")]
    [InlineData("binnabinnabinnabinnabinnabinnabinnabinnabinnabinna123@naver.com")]
    public async Task Test_Join_Email_길이가_범위_밖일때_Fail(string email)
    {
        var code = await mGameUserService.JoinAsync(email, "Alpha123^", "shine");
        
        Assert.Equal($"{EResponseResult.InvalidEmailLength}", $"{code}"); 
    }
    
    [Theory]
    [InlineData("Alph12^")]
    [InlineData("Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha")]
    [InlineData("Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&")]
    [InlineData("Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&")]
    public async Task Test_Join_Password_길이가_범위_밖일때_Fail(string password)
    {
        var code = await mGameUserService.JoinAsync("every5116@naver.com", password, "shine");
        
        Assert.Equal($"{EResponseResult.InvalidPasswordLength}", $"{code}"); 
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
        var code = await mGameUserService.JoinAsync("every5116@naver.com", "Alpha123^", nickname);
        
        Assert.Equal($"{EResponseResult.InvalidNicknameLength}", $"{code}"); 
    }
    
    [Fact]
    public async Task Test_Join_이미_가입된_유저일때_Fail()
    {
        mGameUserRepository
            .ExistsAsync(Arg.Any<string>())
            .Returns(Task.FromResult(true));
        
        var code = await mGameUserService.JoinAsync("every5116@naver.com", "Alpha123^", "shine");
        
        Assert.Equal($"{EResponseResult.EmailAlreadyExists}", $"{code}"); 
    }
    
    [Theory]
    [InlineData("admin@naver.com")]
    [InlineData("shiadminne@binna.company")]
    public async Task Test_Join_Email_금지된_단어일때_Fail(string email)
    {
        mGameUserRepository
            .ExistsAsync(Arg.Any<string>())
            .Returns(Task.FromResult(false));
        
        var code = await mGameUserService.JoinAsync(email, "Alpha123^", "shine");
        
        Assert.Equal( $"{EResponseResult.ForbiddenEmail}", $"{code}"); 
    }
    
    [Theory]
    [InlineData("admin")]
    [InlineData("shiadminne")]
    public async Task Test_Join_Nickname_금지된_단어일때_Fail(string nickname)
    {
        mGameUserRepository
            .ExistsAsync(Arg.Any<string>())
            .Returns(Task.FromResult(false));
        
        var code = await mGameUserService.JoinAsync("every5116@naver.com", "Alpha123^", nickname);
        
        Assert.Equal($"{EResponseResult.ForbiddenNickname}", $"{code}"); 
    }
    
    [Fact]
    public async Task Test_Join_Success()
    {
        mGameUserRepository
            .ExistsAsync(Arg.Any<string>())
            .Returns(Task.FromResult(false));
        
        var code = await mGameUserService.JoinAsync("every5116@naver.com", "Alpha123^", "shine");
        
        Assert.Equal($"{EResponseResult.Success}", $"{code}"); 
    }
    #endregion
    
    #region 로그인
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Test_Login_Email_파라미터가_비었을때_Fail(string? email)
    {
        var result = await mGameUserService.LoginAsync(email, "Alpha123^", "deviceId");
        
        Assert.Equal($"{EResponseResult.EmptyRequiredField}", $"{result.Item1}");
        Assert.Empty(result.Item2.SessionId);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Test_Login_Password_파라미터가_비었을때_Fail(string? password)
    {
        var result = await mGameUserService.LoginAsync("every5116@naver.com", password, "deviceId");
        
        Assert.Equal($"{EResponseResult.EmptyRequiredField}", $"{result.Item1}");
        Assert.Empty(result.Item2.SessionId);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Test_Login_deviceId_파라미터가_비었을때_Fail(string? deviceId)
    {
        var result = await mGameUserService.LoginAsync("every5116@naver.com", "Alpha123", deviceId);
        
        Assert.Equal($"{EResponseResult.EmptyRequiredField}", $"{result.Item1}");
        Assert.Empty(result.Item2.SessionId);
    }
    
    [Fact]
    public async Task Test_Login_Email_가입된_유저를_찾을수없을때_Fail()
    {
        mGameUserRepository
            .FindByEmailAsync(Arg.Any<string>())
            .Returns(Task.FromResult<GameUser?>(null));
        
        var result = await mGameUserService.LoginAsync("every5116@naver.com", "Alpha123^", "deviceId");
        
        Assert.Equal($"{EResponseResult.NoData}", $"{result.Item1}");
        Assert.Empty(result.Item2.SessionId);
    }
    
    [Fact]
    public async Task Test_Login_Success()
    {
        var user = new GameUser("every5116@naver.com", "shine", "Alpha123^");
        
        mGameUserRepository
            .FindByEmailAsync(user.Email)
            .Returns(Task.FromResult<GameUser?>(user));
        
        mSecurityHelper
            .VerifySha512Hash(Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);
        
        var result = await mGameUserService.LoginAsync(user.Email, user.Pw, "deviceId");
        
        Assert.Equal($"{EResponseResult.Success}", $"{result.Item1}");
        Assert.NotEmpty(result.Item2.SessionId);
    }
    
    // TODO EResponseResult.PasswordMismatch 검사
    #endregion
    
    // TODO 하트비트
}