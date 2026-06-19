using Common.KeyValueStore;
using Microsoft.AspNetCore.Authorization;

namespace WebServer.Pipeline;

public class SessionAuthRequirement : IAuthorizationRequirement { }

public class SessionAuthHandler : AuthorizationHandler<SessionAuthRequirement>
{
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly ISessionKeyValueStore mSessionKeyValueStore;

    public SessionAuthHandler(IHttpContextAccessor httpContextAccessor, ISessionKeyValueStore sessionKeyValueStore)
    {
        mHttpContextAccessor = httpContextAccessor;
        mSessionKeyValueStore = sessionKeyValueStore;
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

        if (userId == null || nickname == null)
        {
            context.Fail();
            return;
        }

        var saveSessionId = await mSessionKeyValueStore.GetUserSessionInfoAsync(userId);

        if (!saveSessionId.Equals(http.Session.Id))
        {
            context.Fail();
            return;
        }

        http.Items["userId"] = userId;
        http.Items["nickname"] = nickname;

        context.Succeed(authorizationRequirement);
    }
}