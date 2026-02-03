using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Common.Types;
using Common.Web;
using Protocol.Web.Dtos;
using Shared.Constants;
using Shared.Types;

namespace WebServer.Controllers;

[Route($"{AppConstant.WEB_SERVER_API_BASE}/[controller]")]
[ApiController]
public class ServerDateTimeController : Controller
{
    [HttpPost("set")]
    [AllowAnonymous]
    public ApiResponse SetServerTimeNow([FromBody] SetCustomNowReq req)
    {
        ServerDateTime.SetServerTimeNow(
            ServerDateTime.ToCustomDateTime(req.TargetNow)
        );

        return ApiResponse
            .From(EResponseResult.Success, ELanguage.En);
    }
    
    [HttpPost("reset")]
    [AllowAnonymous]
    public ApiResponse ResetServerTimeNow()
    {
        ServerDateTime.ResetServerTimeNow();
        
        return ApiResponse
            .From(EResponseResult.Success, ELanguage.En);
    }
}