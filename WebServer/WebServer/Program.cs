using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebServer.Common;
using WebServer.Configs;
using WebServer.Models.Repositories;
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
    .AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new() { Title = "API", Version = "v1" });

        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "JWT using Bearer scheme. Example: Bearer {token}"
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

// jwt
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtConfiguration = builder.Configuration.GetSection("JWT");

        var issuer = jwtConfiguration["Issuer"];
        var audience = jwtConfiguration["Audience"];
        var secretKey = jwtConfiguration["SecretKey"];

        if (issuer == null || audience == null || secretKey == null)
        {
            Console.WriteLine("[Critical Error] JWT configuration is missing required fields");
            Environment.Exit(1);
        }

        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

// service 등록(DI 관리 대상 싱글톤 등록)
builder.Services
    .AddSingleton<JwtService>()
    .AddSingleton<UsersService>()
    .AddSingleton<RankingService>()

    .AddSingleton<DbFactory>()
    .AddSingleton<RedisClient>()
    .AddSingleton<UsersDAO>();

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

app.UseAuthentication();
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