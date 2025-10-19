using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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
            ValidIssuer = AppConstants.Jwt.ISSUER,
            ValidAudience = AppConstants.Jwt.AUDIENCE,
            IssuerSigningKey = 
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(AppConstants.Jwt.SECRET_KEY))
        };
    });

// service ���(DI ���� ��� �̱��� ���)
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
