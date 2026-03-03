using Microsoft.AspNetCore.Mvc;
using Shared.Types;

namespace WebServer;

public static class HttpContextExtension
{
    public static string GetUserId(this Controller controller)
    {
        return (string)controller.HttpContext.Items["userId"]!;
    }

    public static string GetNickname(this Controller controller)
    {
        return (string)controller.HttpContext.Items["nickname"]!;
    }

    public static void SetUserSession(
        this IHttpContextAccessor httpContextAccessor, 
        string userId, string nickname)
    {
        httpContextAccessor.HttpContext!.Session.SetString("userId", $"{userId}");
        httpContextAccessor.HttpContext!.Session.SetString("nickname", $"{nickname}");
    }
}