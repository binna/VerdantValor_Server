using Common.Helpers;
using Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Efcore.Repositories;
using Common.Web;
using Shared.Types;
using WebServer.Services;
using Xunit.Abstractions;

namespace WebServer.Test.Unit.Services;

[Collection("GlobalSetup ResponseStatus")]
public class GameUserServiceTest
{
    private readonly ITestOutputHelper mOutput;
    private readonly IKeyValueStore mKeyValueStore;
    private readonly IGameUserRepository mGameUserRepository;
    private readonly GameUserService mGameUserService;
    private readonly ISecurityHelper mSecurityHelper;

    public GameUserServiceTest(ITestOutputHelper output)
    {
        mOutput = output;
        mGameUserRepository = Substitute.For<IGameUserRepository>();
        mKeyValueStore = Substitute.For<IKeyValueStore>();
        mSecurityHelper = Substitute.For<ISecurityHelper>();
        mGameUserService = Substitute.For<GameUserService>(
            Substitute.For<ILogger<GameUserService>>(), 
            Substitute.For<IHttpContextAccessor>(), 
            mGameUserRepository,
            mKeyValueStore,
            mSecurityHelper);
    }

    #region 회원가입
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Test_Join_Email_파라미터가_비었을때_Fail(string? email)
    {
        var responseResult = await mGameUserService
            .JoinAsync(email, "Alpha123^", "shine");
        
        Assert.Equal($"{EResponseResult.EmptyRequiredField}", $"{responseResult}"); 
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Test_Join_Password_파라미터가_비었을때_Fail(string? password)
    {
        var responseResult = await mGameUserService
            .JoinAsync("every5116@naver.com", password, "shine");
        
        Assert.Equal($"{EResponseResult.EmptyRequiredField}", $"{responseResult}"); 
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Test_Join_Nickname_파라미터가_비었을때_Fail(string? nickname)
    {
        var responseResult = await mGameUserService
            .JoinAsync("every5116@naver.com", "Alpha123^", nickname);
        
        Assert.Equal($"{EResponseResult.EmptyRequiredField}", $"{responseResult}"); 
    }
    
    [Theory]
    [InlineData("돼지꾸르륵@naver.com")]
    [InlineData("돼지꾸르륵:(@naver.com")]
    [InlineData("binna:)@naver.com")]
    [InlineData("binna:)><@naver.com")]
    public async Task Test_Join_Email_유효하지않는문자_사용할때_Fail(string email)
    {
        var responseResult = await mGameUserService
            .JoinAsync(email, "Alpha123^", "shine");
        
        Assert.Equal( $"{EResponseResult.InvalidEmailFormat}", $"{responseResult}"); 
    }
    
    [Theory]
    [InlineData("159753456")]
    [InlineData("alphahappy")]
    [InlineData("Alphahappy")]
    [InlineData("Alphahappy123")]
    public async Task Test_Join_Password_유효하지않는문자_사용할때_Fail(string password)
    {
        var responseResult = await mGameUserService
            .JoinAsync("every5116@naver.com", password, "shine");
        
        Assert.Equal( $"{EResponseResult.InvalidPasswordFormat}", $"{responseResult}"); 
    }
    
    [Theory]
    [InlineData("돼지꾸르륵:(")]
    [InlineData("nice:(")]
    [InlineData("nice/")]
    public async Task Test_Join_Nickname_유효하지않는문자_사용할때_Fail(string nickname)
    {
        var responseResult = await mGameUserService
            .JoinAsync("every5116@naver.com", "Alpha123^", nickname);
        
        Assert.Equal($"{EResponseResult.InvalidNicknameFormat}", $"{responseResult}"); 
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
        var responseResult = await mGameUserService
            .JoinAsync(email, "Alpha123^", "shine");
        
        Assert.Equal($"{EResponseResult.InvalidEmailLength}", $"{responseResult}"); 
    }
    
    [Theory]
    [InlineData("Alph12^")]
    [InlineData("Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha")]
    [InlineData("Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&")]
    [InlineData("Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&Alpha123^&")]
    public async Task Test_Join_Password_길이가_범위_밖일때_Fail(string password)
    {
        var responseResult = await mGameUserService
            .JoinAsync("every5116@naver.com", password, "shine");
        
        Assert.Equal($"{EResponseResult.InvalidPasswordLength}", $"{responseResult}"); 
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
        var responseResult = await mGameUserService
            .JoinAsync("every5116@naver.com", "Alpha123^", nickname);
        
        Assert.Equal($"{EResponseResult.InvalidNicknameLength}", $"{responseResult}"); 
    }
    
    [Fact]
    public async Task Test_Join_이미_가입된_유저일때_Fail()
    {
        mGameUserRepository
            .ExistsAsync(Arg.Any<string>())
            .Returns(Task.FromResult(true));
        
        var responseResult = await mGameUserService
            .JoinAsync("every5116@naver.com", "Alpha123^", "shine");
        
        Assert.Equal($"{EResponseResult.EmailAlreadyExists}", $"{responseResult}"); 
    }
    
    [Theory]
    [InlineData("admin@naver.com")]
    [InlineData("shiadminne@binna.company")]
    public async Task Test_Join_Email_금지된_단어일때_Fail(string email)
    {
        mGameUserRepository
            .ExistsAsync(Arg.Any<string>())
            .Returns(Task.FromResult(false));
        
        var responseResult = await mGameUserService
            .JoinAsync(email, "Alpha123^", "shine");
        
        Assert.Equal( $"{EResponseResult.ForbiddenEmail}", $"{responseResult}"); 
    }
    
    [Theory]
    [InlineData("admin")]
    [InlineData("shiadminne")]
    public async Task Test_Join_Nickname_금지된_단어일때_Fail(string nickname)
    {
        mGameUserRepository
            .ExistsAsync(Arg.Any<string>())
            .Returns(Task.FromResult(false));
        
        var responseResult = await mGameUserService
            .JoinAsync("every5116@naver.com", "Alpha123^", nickname);
        
        Assert.Equal($"{EResponseResult.ForbiddenNickname}", $"{responseResult}"); 
    }
    
    [Fact]
    public async Task Test_Join_Success()
    {
        mGameUserRepository
            .ExistsAsync(Arg.Any<string>())
            .Returns(Task.FromResult(false));
        
        var responseResult = await mGameUserService
            .JoinAsync("every5116@naver.com", "Alpha123^", "shine");
        
        Assert.Equal($"{EResponseResult.Success}", $"{responseResult}"); 
    }
    #endregion
    
    #region 로그인
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Test_Login_Email_파라미터가_비었을때_Fail(string? email)
    {
        var responseResult = await mGameUserService.LoginAsync(email, "Alpha123^");
        
        Assert.Equal($"{EResponseResult.EmptyRequiredField}", $"{responseResult}"); 
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Test_Login_Password_파라미터가_비었을때_Fail(string? password)
    {
        var responseResult = await mGameUserService.LoginAsync("every5116@naver.com", password);
        
        Assert.Equal($"{EResponseResult.EmptyRequiredField}", $"{responseResult}"); 
    }
    
    [Fact]
    public async Task Test_Login_Email_가입된_유저를_찾을수없을때_Fail()
    {
        mGameUserRepository
            .FindByEmailAsync(Arg.Any<string>())
            .Returns(Task.FromResult<GameUser?>(null));
        
        var responseResult = await mGameUserService
            .LoginAsync("every5116@naver.com", "Alpha123^");
        
        Assert.Equal($"{EResponseResult.NoData}", $"{responseResult}"); 
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
        
        var responseResult = await mGameUserService
            .LoginAsync(user.Email, user.Pw);
        
        Assert.Equal($"{EResponseResult.Success}", $"{responseResult}"); 
    }
    #endregion
}