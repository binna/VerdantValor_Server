using WebServer.Common;
using WebServer.Models;

namespace WebServer.Services
{
    public class UsersService
    {
        private readonly JwtService jwtService;

        // TODO DB로 이전 예정 =======
        public Dictionary<string, string> userInfos = new();
        // ===========================

        public UsersService(JwtService jwtService)
        {
            this.jwtService = jwtService;

            // TODO DB로 이전 예정 =======
            userInfos.Add("user1", "1234");
            userInfos.Add("user2", "5678");
            userInfos.Add("user3", "9012");
            // ===========================
        }

        public CommonResponse<LoginRes> CheckPassword(string id, string pw)
        {
            userInfos.TryGetValue(id, out var findPw);
            if (string.IsNullOrEmpty(findPw))
            {
                return new CommonResponse<LoginRes>(CommonResponseStatus.emptyUser);
            }

            if (pw != findPw)
            {
                return new CommonResponse<LoginRes>(CommonResponseStatus.notMatchPw);
            }

            var response = new LoginRes() { token = jwtService.CreateToken(id) };
            return new CommonResponse<LoginRes>(CommonResponseStatus.success, response);
        }
    }
}
