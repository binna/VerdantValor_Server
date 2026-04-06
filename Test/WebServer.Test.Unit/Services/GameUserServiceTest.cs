using Common.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Efcore.Repositories;
using Common.Models;
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
        var response = await mGameUserService
            .JoinAsync(email, "1234", "shine");
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.EmptyRequiredField}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Test_Join_Password_파라미터가_비었을때_Fail(string? password)
    {
        var response = await mGameUserService
            .JoinAsync("binna", password, "shine");
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.EmptyRequiredField}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Test_Join_Nickname_파라미터가_비었을때_Fail(string? nickname)
    {
        var response = await mGameUserService
            .JoinAsync("binna", "1234", nickname);
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.EmptyRequiredField}"); 
        Assert.False(response.IsSuccess);
    }

    [Theory]
    [InlineData("돼지꾸르륵")]
    [InlineData("돼지꾸르륵:(")]
    [InlineData("binna:)")]
    [InlineData("binna:)><")]
    public async Task Test_Join_Email_유효하지않는문자_사용할때_Fail(string email)
    {
        var response = await mGameUserService
            .JoinAsync(email, "1234", "shine");
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.EmailAlphabetNumberOnly}"); 
        Assert.False(response.IsSuccess);
    }

    [Theory]
    [InlineData("돼지꾸르륵:(")]
    [InlineData("nice:(")]
    [InlineData("nice/")]
    public async Task Test_Join_Nickname_유효하지않는문자_사용할때_Fail(string nickname)
    {
        var response = await mGameUserService
            .JoinAsync("binna", "1234", nickname);
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.NicknameAlphabetKoreanNumberOnly}"); 
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
        var response = await mGameUserService
            .JoinAsync(email, "1234", "shine");
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.InvalidEmailLength}"); 
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
        var response = await mGameUserService
            .JoinAsync("binna", "1234", nickname);
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.InvalidNicknameLength}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Fact]
    public async Task Test_Join_이미_가입된_유저일때_Fail()
    {
        mGameUserRepository
            .ExistsAsync(Arg.Any<string>())
            .Returns(Task.FromResult(true));
        
        var response = await mGameUserService
            .JoinAsync("binna", "1234", "shine");
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.EmailAlreadyExists}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Theory]
    [InlineData("admin")]
    [InlineData("shiadminne")]
    public async Task Test_Join_Email_금지된_단어일때_Fail(string email)
    {
        mGameUserRepository
            .ExistsAsync(Arg.Any<string>())
            .Returns(Task.FromResult(false));
        
        var response = await mGameUserService
            .JoinAsync(email, "1234", "shine");
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.ForbiddenEmail}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Theory]
    [InlineData("admin")]
    [InlineData("shiadminne")]
    public async Task Test_Join_Nickname_금지된_단어일때_Fail(string nickname)
    {
        mGameUserRepository
            .ExistsAsync(Arg.Any<string>())
            .Returns(Task.FromResult(false));
        
        var response = await mGameUserService
            .JoinAsync("binna", "1234", nickname);
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.ForbiddenNickname}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Fact]
    public async Task Test_Join_Success()
    {
        mGameUserRepository
            .ExistsAsync(Arg.Any<string>())
            .Returns(Task.FromResult(false));
        
        var response = await mGameUserService
            .JoinAsync("binna", "1234", "shine");
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.Success}"); 
        Assert.True(response.IsSuccess);
    }
    #endregion

    #region 로그인
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Test_Login_Email_파라미터가_비었을때_Fail(string? email)
    {
        var response = await mGameUserService
            .LoginAsync(email, "1234");
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.EmptyRequiredField}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Test_Login_Password_파라미터가_비었을때_Fail(string? password)
    {
        var response = await mGameUserService
            .LoginAsync("binna", password);
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.EmptyRequiredField}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Theory]
    [InlineData("돼지꾸르륵")]
    [InlineData("돼지꾸르륵:(")]
    [InlineData("binna:)")]
    [InlineData("binna:)><")]
    public async Task Test_Login_Email_유효하지않는문자_사용할때_Fail(string email)
    {
        var response = await mGameUserService
            .LoginAsync(email, "1234");
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.EmailAlphabetNumberOnly}"); 
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
        var response = await mGameUserService
            .LoginAsync(email, "1234");
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.InvalidEmailLength}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Fact]
    public async Task Test_Login_Email_가입된_유저를_찾을수없을때_Fail()
    {
        mGameUserRepository
            .FindByEmailAsync(Arg.Any<string>())
            .Returns(Task.FromResult<GameUser?>(null));
        
        var response = await mGameUserService
            .LoginAsync("shine94", "1234");
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.NoData}"); 
        Assert.False(response.IsSuccess);
    }
    
    [Fact]
    public async Task Test_Login_Success()
    {
        var user = new GameUser("binna", "shine", "1234");
        
        mGameUserRepository
            .FindByEmailAsync(user.Email)
            .Returns(Task.FromResult<GameUser?>(user));
        
        mSecurityHelper
            .VerifySha512Hash(Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);
        
        var response = await mGameUserService
            .LoginAsync(user.Email, user.Pw);
        
        Assert.Equal($"{response.Code}", $"{(int)EResponseResult.Success}"); 
        Assert.True(response.IsSuccess);
    }
    #endregion
}