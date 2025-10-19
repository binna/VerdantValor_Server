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
        private readonly JwtService jwtService;

        // TODO DB로 이전 예정 =======
        public Dictionary<string, string> userInfos = new();
        // ===========================

        public UsersController(JwtService jwtService)
        {
            this.jwtService = jwtService;

            // TODO DB로 이전 예정 =======
            userInfos.Add("user1", "1234");
            userInfos.Add("user2", "5678");
            userInfos.Add("user3", "9012");
            // ===========================
        }

        [HttpPost("login")]
        public CommonResponse<LoginRes> Login(LoginReq request)
        {
            LoginRes response = new();

            if (string.IsNullOrEmpty(request.id))
            {
                return new CommonResponse<LoginRes>(CommonResponseStatus.emptyId);
            }

            if (string.IsNullOrEmpty(request.pw))
            {
                return new CommonResponse<LoginRes>(CommonResponseStatus.emptyPw);
            }

            userInfos.TryGetValue(request.id, out var pw);
            if (string.IsNullOrEmpty(pw))
            {
                return new CommonResponse<LoginRes>(CommonResponseStatus.emptyUser);
            }

            if (pw != request.pw)
            {
                return new CommonResponse<LoginRes>(CommonResponseStatus.notMatchPw);
            }

            response.token = jwtService.CreateToken(request.id);
            return new CommonResponse<LoginRes>(CommonResponseStatus.success, response);
        }
    }
}
