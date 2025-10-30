using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs;
using WebServer.Common;
using WebServer.Contexts;
using WebServer.Helpers;
using WebServer.Services;

namespace WebServer.Controllers;

[Route(AppConstant.Route.API_BASE)]
[ApiController]
public class UsersController : Controller
{
    private readonly UsersService usersService;

    public UsersController(UsersService usersService)
    {
        this.usersService = usersService;
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

        if (!ValidationHelper.IsValidEmail(request.Email))
            return new ApiResponse(ResponseStatus.emailAlphabetNumericOnly);

        if (request.Email.Length < AppConstant.EAMIL_MIN_LENGTH 
                || request.Email.Length > AppConstant.EAMIL_MAX_LENGTH)
            return new ApiResponse(ResponseStatus.invalEmailLength);

        if (request.Nickname.Length < AppConstant.NICKNAME_MIN_LENGTH
                || request.Nickname.Length > AppConstant.NICKNAME_MAX_LENGTH)
            return new ApiResponse(ResponseStatus.invalNicknameLength);

        return await usersService.Join(request.Email, request.Pw, request.Nickname);
    }

    [HttpPost("login")]
    public async Task<ApiResponse> Login([FromBody] LoginReq request)
    {
        if (string.IsNullOrEmpty(request.Id))
            return new ApiResponse(ResponseStatus.emptyEmail);

        if (string.IsNullOrEmpty(request.Pw))
            return new ApiResponse(ResponseStatus.emptyPw);

        return await usersService.CheckPassword(request.Id, request.Pw);
    }
}