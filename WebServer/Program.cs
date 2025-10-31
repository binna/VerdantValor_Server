using Microsoft.EntityFrameworkCore;
using SharedLibrary.Database.EFCore;
using SharedLibrary.Database.Redis;
using WebServer.DAOs;
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
    Console.WriteLine("Redis and DB connection configurations are missing required fields.");
    Environment.Exit(1);
}

builder.Services.AddPooledDbContextFactory<AppDbContext>(options =>
{
    options.UseMySQL(mysqlConnUrl);
});

try
{
    RedisClient.Instance.Init(host, port);
    Console.WriteLine("Redis connection success.");
}
catch (Exception e)
{
    Console.WriteLine("Redis Connection Fail");
    Console.WriteLine(e);
    Environment.Exit(1);;
}

// service 등록(DI 관리 대상 싱글톤 등록)
builder.Services
    .AddSingleton<WebServer.Infrastructure.DbFactory>()
    // .AddSingleton<RedisClient>()
    .AddSingleton<UsersDao>()
    .AddSingleton<UsersService>()
    .AddSingleton<RankingService>()
    ;

var app = builder.Build();

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