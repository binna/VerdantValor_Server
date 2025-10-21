using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data.Common;
using WebServer.Common;
using WebServer.Contexts;
using WebServer.Models.DTO;
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

            if (request.id.Length < AppConstant.idMinLength 
                    || request.id.Length > AppConstant.idMaxLength)
                return new ApiResponse<JoinRes>(ResponseStatus.invalidIdLength);

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