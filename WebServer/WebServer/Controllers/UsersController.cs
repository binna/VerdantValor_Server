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
        public async Task<ApiResponse<JoinRes>> Join([FromBody] JoinReq request)
        {
            JoinRes response = new();

            if (string.IsNullOrEmpty(request.email))
                return new ApiResponse<JoinRes>(ResponseStatus.emptyEmail);

            if (string.IsNullOrEmpty(request.pw))
                return new ApiResponse<JoinRes>(ResponseStatus.emptyPw);

            if (string.IsNullOrEmpty(request.nickname))
                return new ApiResponse<JoinRes>(ResponseStatus.emptyNickname);

            if (!ValidationHelper.IsValidEmail(request.email))
                return new ApiResponse<JoinRes>(ResponseStatus.emailAlphabetNumericOnly);

            if (request.email.Length < AppConstant.EAMIL_MinLength 
                    || request.email.Length > AppConstant.EAMIL_MAX_LENGTH)
                return new ApiResponse<JoinRes>(ResponseStatus.invalEmailLength);

            if (request.nickname.Length < AppConstant.NICKNAME_MIN_LENGTH
                    || request.nickname.Length > AppConstant.NICKNAME_MAX_LENGTH)
                return new ApiResponse<JoinRes>(ResponseStatus.invalNicknameLength);

            return await usersService.Join(request.email, request.pw, request.nickname);
        }

        [HttpPost("login")]
        public async Task<ApiResponse<LoginRes>> Login([FromBody] LoginReq request)
        {
            LoginRes response = new();

            if (string.IsNullOrEmpty(request.id))
                return new ApiResponse<LoginRes>(ResponseStatus.emptyEmail);

            if (string.IsNullOrEmpty(request.pw))
                return new ApiResponse<LoginRes>(ResponseStatus.emptyPw);

            return await usersService.CheckPassword(request.id, request.pw);
        }
    }
}