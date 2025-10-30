using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs;
using WebServer.Common;
using WebServer.Services;

namespace WebServer.Controllers;

[Route(AppConstant.Route.API_BASE)]
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
        if (string.IsNullOrEmpty(request.Email))
            return new ApiResponse(ResponseStatus.emptyEmail);

        if (string.IsNullOrEmpty(request.Pw))
            return new ApiResponse(ResponseStatus.emptyPw);

        if (string.IsNullOrEmpty(request.Nickname))
            return new ApiResponse(ResponseStatus.emptyNickname);
        
        return await mUsersService.Join(request.Email, request.Pw, request.Nickname);
    }

    [HttpPost("login")]
    public async Task<ApiResponse> Login([FromBody] LoginReq request)
    {
        if (string.IsNullOrEmpty(request.Id))
            return new ApiResponse(ResponseStatus.emptyEmail);

        if (string.IsNullOrEmpty(request.Pw))
            return new ApiResponse(ResponseStatus.emptyPw);

        return await mUsersService.CheckPassword(request.Id, request.Pw);
    }
}