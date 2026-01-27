using Serilog;
using Common.Base;
using VerdantValorShared.Common.Web;

namespace WebServer.Pipeline;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate mNext;
    
    public GlobalExceptionMiddleware(RequestDelegate next)
    {
        mNext = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await mNext(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        Log.Error("Unexpected error occurred in global exception handler. {@info}", 
        new { context.Request.Path, ex.StackTrace });
        
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(
            ApiResponse.From(
                AppEnum.EResponseStatus.UnexpectedError, AppEnum.ELanguage.En));
    }
}