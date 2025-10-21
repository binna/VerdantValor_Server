using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebServer.Common;
using WebServer.Configs;
using WebServer.Contexts;
using WebServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// MVC Controller ���
// Swagger ��Ϻ��� �����ؾ� Swagger���� controller ������ ���� �� ����
builder.Services.AddControllers();

// Swagger ���
builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

// jwt
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = AppConstant.Jwt.ISSUER,
            ValidAudience = AppConstant.Jwt.AUDIENCE,
            IssuerSigningKey = 
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(AppConstant.Jwt.SECRET_KEY))
        };
    });

// service ���(DI ���� ��� �̱��� ���)
builder.Services
    .AddSingleton<JwtService>()
    .AddSingleton<DbFactory>()
    .AddSingleton<UsersService>();

var app = builder.Build();

// ��� �ͼ��� JSON���� ���� ����
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
            
            await context.Response.WriteAsJsonAsync(new AppException(ex));
        });
    });

// HSTS (HTTP Strict Transport Security) : app.UseHsts();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

// Swagger Ȱ��ȭ
app
    .UseSwagger()
    .UseSwaggerUI();

// Attribute ��� ����� Ȱ��ȭ
app.MapControllers();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();