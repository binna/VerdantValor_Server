using Common.Web;
using Microsoft.AspNetCore.Authorization;
using Shared.Types;

namespace WebServer.Pipeline;

public class SessionAuthRequirement : IAuthorizationRequirement { }

public class SessionAuthHandler : AuthorizationHandler<SessionAuthRequirement>
{
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly IKeyValueStore mKeyValueStore;

    public SessionAuthHandler(IHttpContextAccessor httpContextAccessor, IKeyValueStore keyValueStore)
    {
        mHttpContextAccessor = httpContextAccessor;
        mKeyValueStore = keyValueStore;
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

        var saveSessionId = (await mKeyValueStore.GetSessionInfoAsync(userId)).ToString();

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