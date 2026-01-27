using Microsoft.AspNetCore.Http;

namespace Efcore;

public static class AppDbContextExtension
{
    public static AppDbContext GetAppDbContext(this IHttpContextAccessor httpContextAccessor)
    {
        return (AppDbContext)httpContextAccessor.HttpContext!.Items["dbContext"]!;
    }
}