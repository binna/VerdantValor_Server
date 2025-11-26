using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Protocol.Common.Web;

namespace WebServer;

public static class HttpContextExtension
{
    public static string GetUserId(this Controller controller)
    {
        return (string)controller.HttpContext.Items["userId"]!;
    }

    public static AppEnum.ELanguage GetLanguage(this Controller controller)
    {
        return (AppEnum.ELanguage)controller.HttpContext.Items["language"]!;
    }

    public static string GetNickname(this Controller controller)
    {
        return (string)controller.HttpContext.Items["nickname"]!;
    }

    public static void SetUserSession(
        this IHttpContextAccessor httpContextAccessor, 
        string userId, string nickname, string language)
    {
        httpContextAccessor.HttpContext!.Session.SetString("userId", $"{userId}");
        httpContextAccessor.HttpContext!.Session.SetString("nickname", $"{nickname}");
        httpContextAccessor.HttpContext!.Session.SetString("language", $"{language}");
    }
}