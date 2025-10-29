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

            if (string.IsNullOrEmpty(request.Email))
                return new ApiResponse<JoinRes>(ResponseStatus.emptyEmail);

            if (string.IsNullOrEmpty(request.Pw))
                return new ApiResponse<JoinRes>(ResponseStatus.emptyPw);

            if (string.IsNullOrEmpty(request.Nickname))
                return new ApiResponse<JoinRes>(ResponseStatus.emptyNickname);

            if (!ValidationHelper.IsValidEmail(request.Email))
                return new ApiResponse<JoinRes>(ResponseStatus.emailAlphabetNumericOnly);

            if (request.Email.Length < AppConstant.EAMIL_MIN_LENGTH 
                    || request.Email.Length > AppConstant.EAMIL_MAX_LENGTH)
                return new ApiResponse<JoinRes>(ResponseStatus.invalEmailLength);

            if (request.Nickname.Length < AppConstant.NICKNAME_MIN_LENGTH
                    || request.Nickname.Length > AppConstant.NICKNAME_MAX_LENGTH)
                return new ApiResponse<JoinRes>(ResponseStatus.invalNicknameLength);

            return await usersService.Join(request.Email, request.Pw, request.Nickname);
        }

        [HttpPost("login")]
        public async Task<ApiResponse<LoginRes>> Login([FromBody] LoginReq request)
        {
            LoginRes response = new();

            if (string.IsNullOrEmpty(request.Id))
                return new ApiResponse<LoginRes>(ResponseStatus.emptyEmail);

            if (string.IsNullOrEmpty(request.Pw))
                return new ApiResponse<LoginRes>(ResponseStatus.emptyPw);

            return await usersService.CheckPassword(request.Id, request.Pw);
        }
    }
}