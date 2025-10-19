using Microsoft.AspNetCore.Mvc;
using WebServer.Common;
using WebServer.Contexts;
using WebServer.Models;
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

            if (string.IsNullOrEmpty(request.id))
                return new ApiResponse<JoinRes>(ResponseStatus.emptyId);

            if (string.IsNullOrEmpty(request.pw))
                return new ApiResponse<JoinRes>(ResponseStatus.emptyPw);

            // TODO 아이디 길이 제한

            return usersService.Join(request.id, request.pw);
        }

        [HttpPost("login")]
        public ApiResponse<LoginRes> Login([FromBody] LoginReq request)
        {
            LoginRes response = new();

            if (string.IsNullOrEmpty(request.id))
                return new ApiResponse<LoginRes>(ResponseStatus.emptyId);

            if (string.IsNullOrEmpty(request.pw))
                return new ApiResponse<LoginRes>(ResponseStatus.emptyPw);

            return usersService.CheckPassword(request.id, request.pw);
        }
    }
}