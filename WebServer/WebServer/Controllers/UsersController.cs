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

        [HttpPost("login")]
        public CommonResponse<LoginRes> Login([FromBody] LoginReq request)
        {
            LoginRes response = new();

            if (string.IsNullOrEmpty(request.id))
                return new CommonResponse<LoginRes>(CommonResponseStatus.emptyId);

            if (string.IsNullOrEmpty(request.pw))
                return new CommonResponse<LoginRes>(CommonResponseStatus.emptyPw);

            return usersService.CheckPassword(request.id, request.pw);
        }
    }
}