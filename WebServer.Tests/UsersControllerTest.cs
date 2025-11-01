using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedLibrary.DTOs;
using NSubstitute;
using SharedLibrary.Common;
using SharedLibrary.DAOs;
using SharedLibrary.Database;
using SharedLibrary.Database.EFCore;
using WebServer.Controllers;
using WebServer.Services;
using Xunit.Abstractions;

namespace WebServer.Tests;

public class UsersControllerTest
{
    private readonly UsersController mUsersController;
    private readonly ITestOutputHelper mOutput;

    public UsersControllerTest(ITestOutputHelper output)
    {
        var dbContextFactory = new ServiceCollection()
            .AddPooledDbContextFactory<AppDbContext>(opt =>
                opt.UseMySQL(AppSettings.MYSQL_URL))
            .BuildServiceProvider()
            .GetRequiredService<IDbContextFactory<AppDbContext>>();
        
        var logger = Substitute.For<ILogger<UsersService>>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var usersDao = new UsersDao();
        
        var usersService = new UsersService(logger, httpContextAccessor, dbContextFactory, usersDao);
        
        DbFactory.Instance.Init(AppSettings.MYSQL_URL);
        ResponseStatus.Instance.Init();
        
        mUsersController = new UsersController(usersService);
        mOutput = output;
    }

    [Fact]
    public async Task JoinTest()
    {
        var request = new JoinReq { Email = "",  Nickname = "", Pw = "" };
        var response = await mUsersController.Join(request);
        
        mOutput.WriteLine($"{response.IsSuccess}");
        mOutput.WriteLine($"{response.Code}");
        mOutput.WriteLine(response.Message);

        Assert.NotNull(response);
        Assert.False(response.IsSuccess, $"[{response.Code}] {response.Message}");
        
        request = new JoinReq { Email = "banana1",  Nickname = "", Pw = "" };
        response = await mUsersController.Join(request);
        
        mOutput.WriteLine($"{response.IsSuccess}");
        mOutput.WriteLine($"{response.Code}");
        mOutput.WriteLine(response.Message);

        Assert.NotNull(response);
        Assert.False(response.IsSuccess, $"[{response.Code}] {response.Message}");
        
        request = new JoinReq { Email = "banana1",  Nickname = "", Pw = "1234" };
        response = await mUsersController.Join(request);
        
        mOutput.WriteLine($"{response.IsSuccess}");
        mOutput.WriteLine($"{response.Code}");
        mOutput.WriteLine(response.Message);

        Assert.NotNull(response);
        Assert.False(response.IsSuccess, $"[{response.Code}] {response.Message}");
        
        request = new JoinReq { Email = "banana1",  Nickname = "banana1", Pw = "1234" };
        response = await mUsersController.Join(request);
        
        mOutput.WriteLine($"{response.IsSuccess}");
        mOutput.WriteLine($"{response.Code}");
        mOutput.WriteLine(response.Message);

        Assert.NotNull(response);
        Assert.True(response.IsSuccess, $"[{response.Code}] {response.Message}");
    }

    [Fact]
    public async Task LoginTest()
    {
        var request = new LoginReq { Id = "binna", Pw = "1234" };
        var response = await mUsersController.Login(request);

        mOutput.WriteLine($"{response.IsSuccess}");
        mOutput.WriteLine($"{response.Code}");
        mOutput.WriteLine(response.Message);

        Assert.NotNull(response);
        Assert.True(response.IsSuccess, $"[{response.Code}] {response.Message}");
        
        request = new LoginReq { Id = "binna", Pw = "12345" };
        response = await mUsersController.Login(request);
        
        mOutput.WriteLine($"{response.IsSuccess}");
        mOutput.WriteLine($"{response.Code}");
        mOutput.WriteLine(response.Message);
        
        Assert.NotNull(response);
        Assert.False(response.IsSuccess, $"[{response.Code}] {response.Message}");
        
        request = new LoginReq { Id = "notNull", Pw = "1234" };
        response = await mUsersController.Login(request);
        
        mOutput.WriteLine($"{response.IsSuccess}");
        mOutput.WriteLine($"{response.Code}");
        mOutput.WriteLine(response.Message);
        
        Assert.NotNull(response);
        Assert.False(response.IsSuccess, $"[{response.Code}] {response.Message}");
    }
}