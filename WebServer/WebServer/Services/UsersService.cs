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

        public ApiResponse<JoinRes> Join(string id, string pw /* TODO 추후 필요한 항목 추가하기 */)
        {
            if (userInfos.ContainsKey(id))
                return new ApiResponse<JoinRes>(ResponseStatus.idAlreadyExists);

            // TODO 사용 못하는 아이디 모음

            userInfos.Add(id, pw);

            var response = new JoinRes() { token = jwtService.CreateToken(id) };
            return new ApiResponse<JoinRes>(ResponseStatus.success, response);
        }

        public ApiResponse<LoginRes> CheckPassword(string id, string pw)
        {
            userInfos.TryGetValue(id, out var findPw);

            if (string.IsNullOrEmpty(findPw))
                return new ApiResponse<LoginRes>(ResponseStatus.emptyUser);

            if (pw != findPw)
                return new ApiResponse<LoginRes>(ResponseStatus.notMatchPw);

            var response = new LoginRes() { token = jwtService.CreateToken(id) };
            return new ApiResponse<LoginRes>(ResponseStatus.success, response);
        }
    }
}
