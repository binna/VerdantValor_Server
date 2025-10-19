using Microsoft.AspNetCore.Mvc;
using WebServer.Contexts;

namespace WebServer.Controllers
{
    [Route(AppConstants.Routes.API_BASE)]
    [ApiController]
    public class UsersController : Controller
    {
        [HttpGet]
        public IActionResult GetUser() => Ok("Test OK");
    }
}
