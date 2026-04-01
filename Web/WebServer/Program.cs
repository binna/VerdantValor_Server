using Common.Concurrency;
using Common.Driver;
using Common.Helpers;
using Common.Manager;
using Common.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Efcore;
using Efcore.Repositories;
using Redis;
using WebServer.options;
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

Console.WriteLine($"Play Environment : {builder.Environment.EnvironmentName}");

// 내가 사용할 환경 설정 이름
builder.Configuration
    .AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json", 
        optional: false, reloadOnChange: true);

// 세션 설정
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

var securityOption = builder.Configuration
    .GetSection("Security")
    .Get<SecurityOption>(); 

var redisOption = builder.Configuration
    .GetSection("Redis")
    .Get<RedisOption>();

var mysqlOption = builder.Configuration
    .GetSection("MySQL")
    .Get<MysqlOption>();

var serverOption = builder.Configuration
    .GetSection("Server")
    .Get<ServerOption>();

// 설정 객체를 Singleton으로 DI에 등록
builder.Services
    .AddSingleton(securityOption)
    .AddSingleton(redisOption)
    .AddSingleton(mysqlOption)
    .AddSingleton(serverOption);

if (string.IsNullOrWhiteSpace(mysqlOption.Url)
    || string.IsNullOrWhiteSpace(redisOption.Host) 
    || redisOption.Port <= 0
    || string.IsNullOrWhiteSpace(serverOption.Name) 
    || string.IsNullOrEmpty(serverOption.SharedLibraryPath))
{
    Log.Fatal("Configurations are missing required fields. {@fields}", 
        new
        {
            mysqlOption.Url, 
            redisOption.Host, redisOption.Port, 
            serverOption.Name,
            GameDataPath = serverOption.SharedLibraryPath
        });
    Environment.Exit(1);
}

if (string.IsNullOrWhiteSpace(securityOption.ReqEncryptKey))
{
    Log.Fatal("Configurations are missing required fields. {@fields}", 
        new { securityOption.ReqEncryptKey });
    Environment.Exit(1);
}

#region Init
try
{
    // AppContext.BaseDirectory
    //  실행 중인 애플리케이션의 주 실행 파일(host executable)이 위치한 디렉터리 경로를 반환
    var gameDataPath = Path.GetFullPath(
        Path.Combine(serverOption.BaseDir, serverOption.SharedLibraryPath, "GameData", "Data"));
    GameDataManager.LoadAllGameData(gameDataPath);
}
catch (Exception ex)
{
    Log.Fatal(ex, "ResponseStatus setup Fail.");
    Environment.Exit(1);
}
#endregion

// Redis 기반 세션 공유를 위한 분산 캐시 설정
var sessionDatabase = 3;
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = $"{redisOption.Host}:{redisOption.Port},defaultDatabase={sessionDatabase}";
    options.InstanceName = $"{serverOption.Name}_";
});

builder.Services
    .AddPooledDbContextFactory<AppDbContext>(options => 
        options.UseMySQL(mysqlOption.Url));

// 인증 설정, 커스텀한 인가를 사용하기 위해 반드시 필요하여 형식적으로 작업
builder.Services.AddAuthentication("PassThroughAuth")
     .AddScheme<AuthenticationSchemeOptions, PassThroughAuthHandler>(
         "PassThroughAuth", null);

// 인가 정책 설정, Attribute([Authorize]) 기반 정책 적용
builder.Services.AddAuthorization(options => 
    options.AddPolicy(
        "SessionPolicy", 
        policy => policy.Requirements.Add(new SessionAuthRequirement())));

ICacheDriver coreDriver 
    = new RedisCacheDriver(redisOption.Host, $"{redisOption.Port}", 0);
ICacheDriver sessionDriver 
    = new RedisCacheDriver(redisOption.Host, $"{redisOption.Port}", sessionDatabase);
ICacheDriver distributedLockDriver 
    = new RedisCacheDriver(redisOption.Host, $"{redisOption.Port}", 2);
var distributedLockExpiryMs = 1000;

builder.Services
    .AddSingleton<IAuthorizationHandler, SessionAuthHandler>()
    .AddSingleton<IKeyValueStore>(new RedisKeyValueStore(coreDriver, sessionDriver))
    .AddSingleton<IDistributedLock>(new DistributedLock(distributedLockDriver, distributedLockExpiryMs))
    .AddSingleton<IGameUserRepository, GameUserRepository>()
    .AddSingleton<IPurchaseRepository, PurchaseRepository>()
    .AddSingleton<IInventoryRepository, InventoryRepository>()
    .AddSingleton<GameUserService>()
    .AddSingleton<RankingService>()
    .AddSingleton<ItemService>()
    .AddSingleton<StoreService>()
    ;

builder.Services.AddSingleton<ISecurityHelper>(
    _ => new SecurityHelper(securityOption.ReqEncryptKey)
);

var app = builder.Build();

app.UseHttpsRedirection();

// 미들웨어 등록
app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
    app.UseMiddleware<DecryptReqMiddleware>();

app.UseRouting();

// 세션 사용
app.UseSession();           // 세션

// 인증과 인가 정책 사용
app.UseAuthentication();    // 사용자 인증
app.UseAuthorization();     // 정책 기반 인가

//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
}

app.MapControllers();

app.Run();