using Serilog;
using SharedLibrary.Common;
using SharedLibrary.Protocol.Common.Web;

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
            new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.UnexpectedError, AppEnum.ELanguage.Ko)));
    }
}