using Microsoft.AspNetCore.Mvc;
using WebServer.Common;
using WebServer.Contexts;
using WebServer.DTOs;
using WebServer.Helpers;
using WebServer.Services;

namespace WebServer.Controllers
{
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
        public ApiResponse<JoinRes> Join([FromBody] JoinReq request)
        {
            JoinRes response = new();

            if (string.IsNullOrEmpty(request.email))
                return new ApiResponse<JoinRes>(ResponseStatus.emptyEmail);

            if (string.IsNullOrEmpty(request.pw))
                return new ApiResponse<JoinRes>(ResponseStatus.emptyPw);

            if (!ValidationHelper.IsValidEmail(request.email))
                return new ApiResponse<JoinRes>(ResponseStatus.emailAlphabetNumericOnly);

            if (request.email.Length < AppConstant.emailMinLength 
                    || request.email.Length > AppConstant.emailMaxLength)
                return new ApiResponse<JoinRes>(ResponseStatus.invalidLength);

            if (!string.IsNullOrEmpty(request.nickname))
            {
                if (request.nickname.Length < AppConstant.nicknameMinLength
                    || request.nickname.Length > AppConstant.nicknameMaxLength)
                    return new ApiResponse<JoinRes>(ResponseStatus.invalidLength);
            }

            return usersService.Join(request.email, request.pw, request.nickname);
        }

        [HttpPost("login")]
        public ApiResponse<LoginRes> Login([FromBody] LoginReq request)
        {
            LoginRes response = new();

            if (string.IsNullOrEmpty(request.id))
                return new ApiResponse<LoginRes>(ResponseStatus.emptyEmail);

            if (string.IsNullOrEmpty(request.pw))
                return new ApiResponse<LoginRes>(ResponseStatus.emptyPw);

            return usersService.CheckPassword(request.id, request.pw);
        }
    }
}