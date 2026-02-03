using Microsoft.AspNetCore.Authorization;
using Redis;
using Shared.Types;

namespace WebServer.Pipeline;

public class SessionAuthRequirement : IAuthorizationRequirement { }

public class SessionAuthHandler : AuthorizationHandler<SessionAuthRequirement>
{
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly IRedisClient mRedisClient;

    public SessionAuthHandler(IHttpContextAccessor httpContextAccessor, IRedisClient redisClient)
    {
        mHttpContextAccessor = httpContextAccessor;
        mRedisClient = redisClient;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SessionAuthRequirement authorizationRequirement)
    {
        var http = mHttpContextAccessor.HttpContext;

        if (http == null)
        {
            context.Fail();
            return;
        }

        var userId = http.Session.GetString("userId");
        var nickname = http.Session.GetString("nickname");
        var langStr = http.Session.GetString("language");

        if (!Enum.TryParse<ELanguage>(langStr, out var language))
            language = ELanguage.En;

        if (userId == null || nickname == null)
        {
            context.Fail();
            return;
        }

        var saveSessionId = (await mRedisClient.GetSessionInfoAsync(userId)).ToString();

        if (!saveSessionId.Equals(http.Session.Id))
        {
            context.Fail();
            return;
        }

        http.Items["UserId"] = userId;
        http.Items["nickname"] = nickname;
        http.Items["language"] = language;

        context.Succeed(authorizationRequirement);
    }
}