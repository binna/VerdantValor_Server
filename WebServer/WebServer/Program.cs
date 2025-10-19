using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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
            ValidIssuer = AppConstants.Jwt.ISSUER,
            ValidAudience = AppConstants.Jwt.AUDIENCE,
            IssuerSigningKey = 
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(AppConstants.Jwt.SECRET_KEY))
        };
    });

// service 등록(DI 관리 대상 싱글톤 등록)
builder.Services
    .AddSingleton<JwtService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//    // The default HSTS value is 30 days.
//    // You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

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
