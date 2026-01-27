using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Common.Base;
using VerdantValorShared.Common.Web;
using VerdantValorShared.DTOs.Web;
using Common.Types;

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
            .From(AppEnum.EResponseStatus.Success, AppEnum.ELanguage.En);
    }
    
    [HttpPost("reset")]
    [AllowAnonymous]
    public ApiResponse ResetServerTimeNow()
    {
        ServerDateTime.ResetServerTimeNow();
        
        return ApiResponse
            .From(AppEnum.EResponseStatus.Success, AppEnum.ELanguage.En);
    }
}