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

// MVC Controller 등록
// Swagger 등록보다 먼저해야 Swagger에서 controller 정보를 얻을 수 있음
builder.Services.AddControllers();

// Swagger 등록
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

// service 등록(DI 관리 대상 싱글톤 등록)
builder.Services
    .AddSingleton<JwtService>()
    .AddSingleton<DbFactory>()
    .AddSingleton<UsersService>();

var app = builder.Build();

// 모든 익셉션 JSON으로 형식 통일
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

// Swagger 활성화
app
    .UseSwagger()
    .UseSwaggerUI();

// Attribute 기반 라우팅 활성화
app.MapControllers();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();