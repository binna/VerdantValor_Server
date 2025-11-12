using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SharedLibrary.Common;
using SharedLibrary.Efcore;
using SharedLibrary.Efcore.Repository;
using SharedLibrary.Efcore.Transaction;
using SharedLibrary.Protocol.Common;
using SharedLibrary.Redis;
using WebServer;
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

// 세션 설정
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".VerdantValor.Session";
});

// 현재 HTTP 요청(Context)에 대한 정보
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
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

#if LIVE
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
#endif

builder.Services
    .AddPooledDbContextFactory<AppDbContext>(options => 
        options.UseMySQL(mysqlConnUrl));

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

// service 등록(DI 관리 대상 싱글톤 등록)
builder.Services
    .AddSingleton<IRedisClient, ConfigRedisClient>()
    .AddSingleton<IUsersRepository, UsersRepository>()
    .AddSingleton<IUsersServiceTransaction, UsersServiceTransaction>()
    .AddSingleton<UsersService>()
    .AddSingleton<RankingService>()
    ;

var app = builder.Build();

app
    .UseExceptionHandler(exceptionHandlerApp =>
    {
        exceptionHandlerApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";

            var exceptionHandlerPathFeature =
                context.Features.Get<IExceptionHandlerPathFeature>();
            var ex = exceptionHandlerPathFeature?.Error;

            Log.Error(ex, "Unexpected error occurred in global exception handler.");
            
            await context.Response.WriteAsJsonAsync(
                new ApiResponse(
                    ResponseStatus.FromResponseStatus(
                        EResponseStatus.UnexpectedError, AppEnum.ELanguage.En)));
        });
    });

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();       // 여기서 세션 기능 활성화

app.UseAuthentication();
app.UseAuthorization(); // 여기서 AuthorizationHandler 실행됨

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
}

app.MapControllers();

app.Run();