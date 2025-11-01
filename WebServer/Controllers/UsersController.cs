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
        AppConstant.ELanguage language;
        
        try
        {
            language = Enum.Parse<AppConstant.ELanguage>(request.Language);
        }
        catch (Exception)
        {
            language = AppConstant.ELanguage.En;
        }
        
        if (string.IsNullOrEmpty(request.Email))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyEmail, language));

        if (string.IsNullOrEmpty(request.Pw))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyPw, language));

        if (string.IsNullOrEmpty(request.Nickname))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyNickname, language));
        
        return await mUsersService.Join(request.Email, request.Pw, request.Nickname, language);
    }

    [HttpPost("login")]
    public async Task<ApiResponse> Login([FromBody] LoginReq request)
    {
        AppConstant.ELanguage language;
        
        try
        {
            language = Enum.Parse<AppConstant.ELanguage>(request.Language);
        }
        catch (Exception)
        {
            language = AppConstant.ELanguage.En;
        }
        
        if (string.IsNullOrEmpty(request.Id))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyEmail, language));

        if (string.IsNullOrEmpty(request.Pw))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyPw, language));

        return await mUsersService.CheckPassword(request.Id, request.Pw, language);
    }
}