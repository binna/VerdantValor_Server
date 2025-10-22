using WebServer.Common;
using WebServer.DTOs;
using WebServer.Helpers;
using WebServer.Models;
using WebServer.Models.Repositories;

namespace WebServer.Services
{
    public class UsersService
    {
        private readonly ILogger<UsersService> logger;
        private readonly JwtService jwtService;
        private readonly UsersDAO usersDao;

        // TODO DB로 이전 예정 =======
        public HashSet<string> bannedIds = new();
        // ===========================

        public UsersService(ILogger<UsersService> logger, 
                            JwtService jwtService, 
                            UsersDAO usersDao)
        {
            this.logger = logger;
            this.jwtService = jwtService;
            this.usersDao = usersDao;

            bannedIds.Add("admin");
        }

        public ApiResponse<JoinRes> Join(string email, string pw, string? nickname)
        {
            if (usersDao.existsByEmail(email))
                return new ApiResponse<JoinRes>(ResponseStatus.emailAlreadyExists);

            bool containsForbiddenWord = 
                bannedIds.Any(word => email.Contains(word, StringComparison.OrdinalIgnoreCase));

            if (containsForbiddenWord)
                return new ApiResponse<JoinRes>(ResponseStatus.forbiddenEmail);

            string hashpw = HashHelper.ComputeSHA512Hash(pw);

            if (!usersDao.Save(nickname, email, hashpw))
                return new ApiResponse<JoinRes>(ResponseStatus.dbError);

            var response = new JoinRes() { token = jwtService.CreateToken(email) };
            return new ApiResponse<JoinRes>(ResponseStatus.success, response);
        }

        public ApiResponse<LoginRes> CheckPassword(string email, string pw)
        {
            Users? user = usersDao.FindByEmail(email);

            if (user == null)
                return new ApiResponse<LoginRes>(ResponseStatus.emptyUser);

            if (!HashHelper.VerifySHA512Hash(pw, user.pw))
                return new ApiResponse<LoginRes>(ResponseStatus.notMatchPw);

            var response = new LoginRes() { token = jwtService.CreateToken(email) };
            return new ApiResponse<LoginRes>(ResponseStatus.success, response);
        }
    }
}
