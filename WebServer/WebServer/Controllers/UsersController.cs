using Microsoft.AspNetCore.Mvc;
using WebServer.Contexts;
using WebServer.Models;
using WebServer.Services;

namespace WebServer.Controllers
{
    [Route(AppConstants.Route.API_BASE)]
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
        public LoginRes Login(LoginReq request)
        {
            LoginRes response = new();

            if (string.IsNullOrEmpty(request.id))
            {
                Console.WriteLine("1");
                return response;
            }

            if (string.IsNullOrEmpty(request.pw))
            {
                Console.WriteLine("2");
                return response;
            }

            userInfos.TryGetValue(request.id, out var password);
            Console.WriteLine(password);

            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("4");
                return response;
            }

            if (password != request.pw)
            {
                Console.WriteLine("3");
                return response;
            }

            response.token = jwtService.CreateToken(request.id);
            return response;
        }
    }
}
