#if DEVELOPMENT
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Common;
using SharedLibrary.Protocol.Common;
using SharedLibrary.Protocol.DTOs;
using SharedLibrary.Utils;

namespace WebServer.Controllers;

[Route($"{AppConstant.WEB_SERVER_API_BASE}/[controller]")]
[ApiController]
public class CustomDateTimeController : Controller
{
    [HttpPost("set")]
    public ApiResponse SetCustomNow([FromBody] SetCustomNowReq req)
    {
        CustomDateTime.SetCustomNow(
            CustomDateTime.ToCustomDateTime(req.TargetNow)
        );
        
        return new ApiResponse(ResponseStatus.FromResponseStatus(
            EResponseStatus.Success, AppEnum.ELanguage.Ko));
    }
    
    [HttpPost("reset")]
    public ApiResponse ResetCustomNow()
    {
        CustomDateTime.ResetCustomNow();
        
        return new ApiResponse(ResponseStatus.FromResponseStatus(
            EResponseStatus.Success, AppEnum.ELanguage.Ko));
    }
}
#endif
