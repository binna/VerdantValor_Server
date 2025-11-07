using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SharedLibrary.Common;
using SharedLibrary.DTOs;
using Xunit.Abstractions;

namespace WebServer.Test.Integration;

public class UsersApiIntegrationTest 
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> mWebApplicationFactory;
    private readonly HttpClient mHttpClient;
    private readonly ITestOutputHelper mOutput;

    public UsersApiIntegrationTest(
        WebApplicationFactory<Program> webApplicationFactory, 
        ITestOutputHelper output)
    {
        mWebApplicationFactory = webApplicationFactory;
        mHttpClient = mWebApplicationFactory.CreateClient();
        mOutput = output;
    }

    [Theory]
    [InlineData($"{AppConstant.WEB_SERVER_API_BASE}/Users/Auth")]
    public async Task AuthLogin(string url)
    {
        var request = new AuthReq
        {
            AuthType = $"{AppConstant.EAuthType.Login}", 
            Email = "binna", 
            Pw = "1234"
        };

        var content = JsonSerializer.Serialize(request);
        
        var response = await mHttpClient.PostAsync(
            url, new StringContent(
                content, Encoding.UTF8, "application/json"));
        
        mOutput.WriteLine($"{response.Headers}");
        mOutput.WriteLine($"{await response.Content.ReadAsStringAsync()}");
        mOutput.WriteLine($"{response}");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}