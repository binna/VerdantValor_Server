using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SharedLibrary.Common;
using SharedLibrary.Efcore;
using SharedLibrary.Efcore.Repository;
using SharedLibrary.Redis;
using WebServer;
using WebServer.Pipeline;
using WebServer.Services;

var builder = WebApplication.CreateBuilder(args);

// 환경설정에 정의된 Serilog 설정으로 Logger 구성
// ASP.NET Core의 기본 ILogger를 Serilog로 대체하도록 설정
Log.Logger = new LoggerConfiguration().ReadFrom
    .Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();   // 기존 기본 로깅 공급자 제거 후
builder.Host.UseSerilog();          // Serilog를 로거로 사용

// 내가 사용할 환경 설정 이름
builder.Configuration
    .AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json", 
        optional: false, reloadOnChange: true);

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".VerdantValor.Session";
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers(options =>
{
    options.Conventions.Add(
        new ExcludeControllerConvention(
            builder.Environment, "ServerDateTime"));
    options.Filters.Add<DBContextActionFilter>();
});

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

var mysqlConnUrl = builder.Configuration["DB:MySQL:Url"];
var host = builder.Configuration["DB:Redis:Host"];
var port = builder.Configuration["DB:Redis:Port"];
var serverName = builder.Configuration["Server:Name"];

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = $"{host}:{port},defaultDatabase=3";
    options.InstanceName = $"{serverName}_";
});

if (string.IsNullOrWhiteSpace(mysqlConnUrl)
    || string.IsNullOrWhiteSpace(host) 
    || string.IsNullOrWhiteSpace(port)
    || string.IsNullOrWhiteSpace(serverName))
{
    Log.Fatal("Configurations are missing required fields. {@fields}", 
        new { mysqlConnUrl, host, port, serverName });
    Environment.Exit(1);
}

builder.Services
    .AddPooledDbContextFactory<AppDbContext>(options => 
        options.UseMySQL(mysqlConnUrl));

#region Init
try
{
    // AppContext.BaseDirectory
    // 실행 중인 애플리케이션의 주 실행 파일(host executable)이 위치한 디렉터리 경로를 반환
    var baseDir = AppContext.BaseDirectory;
    var path = Path.GetFullPath(
        Path.Combine(baseDir, AppConstant.SHARED_LIBRARY_PATH, "GameData", "Data", "ResponseStatus.json"));
    ResponseStatus.Init(path);
    Log.Information("Response Status setup success. {@path}", new { jsonPath = path });
}
catch (Exception ex)
{
    Log.Fatal(ex, "ResponseStatus setup Fail.");
    Environment.Exit(1);
}

if (!builder.Environment.IsDevelopment())
{
    var reqEncryptKey = builder.Configuration["ReqEncryptKey"];
    if (string.IsNullOrWhiteSpace(reqEncryptKey))
    {
        Log.Fatal("Configurations are missing required fields. {@fields}", 
            new { reqEncryptKey });
        Environment.Exit(1);
    }

    try
    {
        AppReadonly.Init(reqEncryptKey);
        Log.Information("Request Encrypt Key setup success. {@reqEncryptKey}", new { reqEncryptKey });
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "ResponseStatus setup Fail.");
        Environment.Exit(1);
    }
}
#endregion

builder.Services.AddAuthentication("PassThroughAuth")
     .AddScheme<AuthenticationSchemeOptions, PassThroughAuthHandler>(
         "PassThroughAuth", null);

builder.Services.AddAuthorization(options => 
    options.AddPolicy(
        "SessionPolicy", 
        policy => policy.Requirements.Add(new SessionAuthRequirement())));

// DI 관리 대상 싱글톤 등록
builder.Services
    .AddSingleton<IAuthorizationHandler, SessionAuthHandler>()
    .AddSingleton<IRedisClient, ConfigRedisClient>()
    .AddSingleton<IUsersRepository, UsersRepository>()
    .AddSingleton<UsersService>()
    .AddSingleton<RankingService>()
    ;

var app = builder.Build();

app.UseHttpsRedirection();

// 미들웨어 등록
app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
    app.UseMiddleware<DecryptReqMiddleware>();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
}

app.MapControllers();

app.Run();