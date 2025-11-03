using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Common;
using SharedLibrary.DTOs;
using WebServer.Common;
using WebServer.Services;

namespace WebServer.Controllers;

[Route(AppConstant.WEB_SERVER_API_BASE)]
[ApiController]
public class UsersController : Controller
{
    private readonly UsersService mUsersService;

    public UsersController(UsersService usersService)
    {
        mUsersService = usersService;
    }

    [HttpPost("join")]
    public async Task<ApiResponse> Join([FromBody] JoinReq request)
    {
        if (!Enum.TryParse<AppConstant.ELanguage>(request.Language, out var language))
            language = AppConstant.ELanguage.En;
        
        if (string.IsNullOrWhiteSpace(request.Email))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyEmail, language));

        if (string.IsNullOrWhiteSpace(request.Pw))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyPw, language));

        if (string.IsNullOrWhiteSpace(request.Nickname))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyNickname, language));
        
        return await mUsersService.Join(request.Email, request.Pw, request.Nickname, language);
    }

    [HttpPost("login")]
    public async Task<ApiResponse> Login([FromBody] LoginReq request)
    {
        if (!Enum.TryParse<AppConstant.ELanguage>(request.Language, out var language))
            language = AppConstant.ELanguage.En;
        
        if (string.IsNullOrWhiteSpace(request.Email))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyEmail, language));

        if (string.IsNullOrWhiteSpace(request.Pw))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyPw, language));

        return await mUsersService.CheckPassword(request.Email, request.Pw, language);
    }
}