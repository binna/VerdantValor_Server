using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Common;
using SharedLibrary.DAOs;
using SharedLibrary.Database;
using SharedLibrary.Database.EFCore;
using SharedLibrary.Database.Redis;
using WebServer.Common;
using WebServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".VerdantValor.Session";
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

var mysqlConnUrl = builder.Configuration["DB:MySQL:Url"];
var host = builder.Configuration["DB:Redis:Host"];
var port = builder.Configuration["DB:Redis:Port"];

if (mysqlConnUrl == null ||  host == null || port == null)
{
    Console.WriteLine("[Critical Fail] Redis and DB connection configurations are missing required fields.");
    Environment.Exit(1);
}

builder.Services.AddPooledDbContextFactory<AppDbContext>(options =>
{
    options.UseMySQL(mysqlConnUrl);
});

try
{
    RedisClient.Instance.Init(host, port);
    Console.WriteLine("[info] Redis connection success.");
}
catch (Exception e)
{
    Console.WriteLine("[Critical Fail] Redis Connection Fail");
    Console.WriteLine(e);
    Environment.Exit(1);;
}

try
{
    DbFactory.Instance.Init(mysqlConnUrl);
    Console.WriteLine("[info] DB connection success.");
}
catch (Exception e)
{
    Console.WriteLine("[Critical Fail] DB Connection Fail");
    Console.WriteLine(e);
    Environment.Exit(1);;
}

try
{
    ResponseStatus.Instance.Init();
    Console.WriteLine("[info] ResponseStatus setup success.");
}
catch (Exception e)
{
    Console.WriteLine("[Critical Fail] ResponseStatus setup Fail");
    Console.WriteLine(e);
    Environment.Exit(1);;
}

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

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
}

app.MapControllers();

app.Run();

// TODO 익셉션 핸들러 만들기
// show는 일반 문구, 서버에만 디테일 남기기