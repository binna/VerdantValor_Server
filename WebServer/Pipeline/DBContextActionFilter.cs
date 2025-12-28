using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Common;
using SharedLibrary.Efcore;
using SharedLibrary.Protocol.Common.Web;

namespace WebServer.Pipeline;

public class DBContextActionFilter : ActionFilterAttribute
{
    private readonly IDbContextFactory<AppDbContext> mDbContextFactory;
    
    public DBContextActionFilter(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        mDbContextFactory = dbContextFactory;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 매번 새 커넥션을 쓰는 것처럼 보이지만
        // 드라이브에서 커넥션 풀까지 코드가 짜여 있기 때문에
        // 커넥션 풀 재사용 한다
        await using var dbContext = await mDbContextFactory.CreateDbContextAsync();
        context.HttpContext.Items.Add("dbContext", dbContext);
        context.HttpContext.Items.Add("isChange", false);
        
        await next();

        if (dbContext.ChangeTracker.HasChanges())
        {
            var result = await dbContext.SaveChangesAsync();

            if (result <= 0)
            {
                context.Result = new ContentResult
                {
                    StatusCode = StatusCodes.Status200OK,
                    ContentType = "application/json",
                    Content = JsonSerializer.Serialize(
                        ApiResponse.From(AppEnum.EResponseStatus.DbError, AppEnum.ELanguage.En))
                };
            }
        }
    }
}