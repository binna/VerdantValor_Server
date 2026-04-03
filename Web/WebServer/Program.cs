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
using WebServer.types;

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

var dbOption = builder.Configuration
    .GetSection("Database")
    .Get<DatabaseOption>();

var serverOption = builder.Configuration
    .GetSection("Server")
    .Get<ServerOption>();

var pathOption = builder.Configuration
    .GetSection("Path")
    .Get<PathOption>();

// 설정 객체를 Singleton으로 DI에 등록
builder.Services
    .AddSingleton(securityOption)
    .AddSingleton(serverOption);

if (redisOption.Enabled)
{
    if (string.IsNullOrWhiteSpace(redisOption.Host) 
        || redisOption.Port <= 0
        || redisOption.CoreDbNum < 0
        || redisOption.SessionDbNum < 0
        || redisOption.LockDbNum < 0)
    {
        Log.Fatal("Invalid Redis configuration. {@fields}", 
            new
            {
                redisOption.Host,
                redisOption.Port,
                redisOption.CoreDbNum,
                redisOption.SessionDbNum,
                redisOption.LockDbNum
            });
        Environment.Exit(1);
    }
}

if (dbOption.Mode == EDatabaseMode.MySql)
{
    if (string.IsNullOrWhiteSpace(dbOption.Url))
    {
        Log.Fatal("Invalid MySql configuration. {@fields}", 
            new
            {
                dbOption.Url 
            });
        Environment.Exit(1);
    }
}


if (string.IsNullOrWhiteSpace(serverOption.Name) 
    || string.IsNullOrEmpty(pathOption.SharedLibrary) 
    || redisOption.LockExpiryMs <= 0)
{
    Log.Fatal("Invalid configuration. {@fields}", 
        new
        {
            serverOption.Name,
            pathOption.SharedLibrary,
            redisOption.LockExpiryMs
        });
    Environment.Exit(1);
}

if (string.IsNullOrWhiteSpace(securityOption.ReqEncryptKey) 
    || !SecurityHelper.IsValidEncryptionKey(securityOption.ReqEncryptKey))
{
    Log.Fatal("Encryption key configuration is invalid.");
    Environment.Exit(1);
}

#region Init
try
{
    GameDataManager.LoadAllGameData(pathOption.GameData);
}
catch (Exception ex)
{
    Log.Fatal(ex, "ResponseStatus setup Fail.");
    Environment.Exit(1);
}
#endregion

if (redisOption.Enabled)
{
    var redisUrl = $"{redisOption.Host}:{redisOption.Port},defaultDatabase={redisOption.SessionDbNum}";

    // Redis 기반 세션 공유를 위한 분산 캐시 설정
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisUrl;
        options.InstanceName = $"{serverOption.Name}_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

if (dbOption.Mode == EDatabaseMode.InMemory)
{
    builder.Services
        .AddPooledDbContextFactory<AppDbContext>(options =>
            options.UseInMemoryDatabase(serverOption.Name));
}
else
{
    builder.Services
        .AddPooledDbContextFactory<AppDbContext>(options => 
            options.UseMySQL(dbOption.Url));
}

// 인증 설정, 커스텀한 인가를 사용하기 위해 반드시 필요하여 형식적으로 작업
builder.Services.AddAuthentication("PassThroughAuth")
     .AddScheme<AuthenticationSchemeOptions, PassThroughAuthHandler>(
         "PassThroughAuth", null);

// 인가 정책 설정, Attribute([Authorize]) 기반 정책 적용
builder.Services.AddAuthorization(options => 
    options.AddPolicy(
        "SessionPolicy", 
        policy => policy.Requirements.Add(new SessionAuthRequirement())));

ICacheDriver coreDriver;
ICacheDriver sessionDriver;
ICacheDriver distributedLockDriver;

if (redisOption.Enabled)
{
    coreDriver = new RedisCacheDriver(redisOption.Host, $"{redisOption.Port}", redisOption.CoreDbNum);
    sessionDriver = new RedisCacheDriver(redisOption.Host, $"{redisOption.Port}", redisOption.SessionDbNum);
    distributedLockDriver = new RedisCacheDriver(redisOption.Host, $"{redisOption.Port}", redisOption.LockDbNum);
}
else
{
    coreDriver = new FakeCacheDriver();
    sessionDriver = new FakeCacheDriver();
    distributedLockDriver = new FakeCacheDriver();
}

builder.Services
    .AddSingleton<ISecurityHelper>(new SecurityHelper(securityOption.ReqEncryptKey))
    .AddSingleton<IAuthorizationHandler, SessionAuthHandler>()
    .AddSingleton<IKeyValueStore>(new RedisKeyValueStore(coreDriver, sessionDriver))
    .AddSingleton<IDistributedLock>(new DistributedLock(distributedLockDriver, redisOption.LockExpiryMs))
    .AddSingleton<IGameUserRepository, GameUserRepository>()
    .AddSingleton<IPurchaseRepository, PurchaseRepository>()
    .AddSingleton<IInventoryRepository, InventoryRepository>()
    .AddSingleton<GameUserService>()
    .AddSingleton<RankingService>()
    .AddSingleton<ItemService>()
    .AddSingleton<StoreService>()
    ;

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
}

app.MapControllers();

app.Run();