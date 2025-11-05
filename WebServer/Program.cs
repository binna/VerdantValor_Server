using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SharedLibrary.Common;
using SharedLibrary.DAOs;
using SharedLibrary.Database;
using SharedLibrary.Database.EFCore;
using SharedLibrary.Database.Redis;
using WebServer.Common;
using WebServer.Services;

var builder = WebApplication.CreateBuilder(args);

// 환경설정에 정의된 Serilog 설정으로 Logger 구성
// ASP.NET Core의 기본 ILogger를 Serilog로 대체하도록 설정
Log.Logger = new LoggerConfiguration().ReadFrom
    .Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();   // 기존 기본 로깅 공급자 제거 후
builder.Host.UseSerilog();          // Serilog를 로거로 사용

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
    || string.IsNullOrWhiteSpace(serverName ))
{
    Log.Fatal("Configurations are missing required fields. {@fields}", 
        new { mysqlConnUrl, host, port, serverName});
    Environment.Exit(1);
}

builder.Services
    .AddPooledDbContextFactory<AppDbContext>(options => 
        options.UseMySQL(mysqlConnUrl));

#region 싱글톤
try
{
    RedisClient.Instance.Init(host, port);
    Log.Information("Redis connection success. {@context}",
        new { host, port });
}
catch (Exception e)
{
    Log.Fatal(e, "Redis Connection Fail. {@context}",
        new { host, port });
    Environment.Exit(1);;
}

try
{
    DbFactory.Instance.Init(mysqlConnUrl);
    Log.Information("DB connection success. {@context}",
        new { mysqlConnUrl });
}
catch (Exception e)
{
    Log.Fatal(e, "DB Connection Fail. {@context}",
        new { mysqlConnUrl });
    Environment.Exit(1);;
}

try
{
    ResponseStatus.Instance.Init();
    Log.Information("ResponseStatus setup success.");
}
catch (Exception e)
{
    Log.Fatal(e, "ResponseStatus setup Fail.");
    Environment.Exit(1);
}
#endregion

// service 등록(DI 관리 대상 싱글톤 등록)
builder.Services
    .AddSingleton<UsersDao>()
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

            Console.WriteLine(ex);
            
            await context.Response.WriteAsJsonAsync(
                new ApiResponse(
                    ResponseStatus.FromResponseStatus(
                        EResponseStatus.UnexpectedError, AppConstant.ELanguage.En)));
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