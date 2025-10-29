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

        private readonly UsersDAO usersDao;

        // TODO DB에서 관리하는 부분 제작하기 =======
        public HashSet<string> bannedEmails = new();
        public HashSet<string> bannedNicknames = new();
        // ========================================

        public UsersService(ILogger<UsersService> logger, 
                            UsersDAO usersDao)
        {
            this.logger = logger;

            this.usersDao = usersDao;

            // ==========================================
            bannedEmails.Add("admin");
            bannedNicknames.Add("admin");
            // ==========================================
        }

        public async Task<ApiResponse<JoinRes>> Join(string email, string pw, string nickname)
        {
            if (await usersDao.ExistsByEmail(email))
                return new ApiResponse<JoinRes>(ResponseStatus.emailAlreadyExists);

            bool containsForbiddenWord =
                bannedEmails.Any(word => email.Contains(word, StringComparison.OrdinalIgnoreCase));

            if (containsForbiddenWord)
                return new ApiResponse<JoinRes>(ResponseStatus.forbiddenEmail);

            containsForbiddenWord =
               bannedNicknames.Any(word => nickname.Contains(word, StringComparison.OrdinalIgnoreCase));

            if (containsForbiddenWord)
                return new ApiResponse<JoinRes>(ResponseStatus.forbiddenNickname);

            string hashpw = HashHelper.ComputeSHA512Hash(pw);

            if (!await usersDao.Save(nickname, email, hashpw))
            {
                logger.LogError("Database error occurred while saving user information.");
                return new ApiResponse<JoinRes>(ResponseStatus.dbError);
            }

            var user = await usersDao.FindByEmail(email);
            if (user == null)
            {
                logger.LogError("Database error occurred while finding user information.");
                return new ApiResponse<JoinRes>(ResponseStatus.dbError); 
            }

            var response = new JoinRes() { Token = "" };
            return new ApiResponse<JoinRes>(ResponseStatus.success, response);
        }

        public async Task<ApiResponse<LoginRes>> CheckPassword(string email, string pw)
        {
            Users? user = await usersDao.FindByEmail(email);

            if (user == null)
                return new ApiResponse<LoginRes>(ResponseStatus.emptyUser);

            if (!HashHelper.VerifySHA512Hash(pw, user.pw))
                return new ApiResponse<LoginRes>(ResponseStatus.notMatchPw);

            var response = new LoginRes() { Token = "" };
            return new ApiResponse<LoginRes>(ResponseStatus.success, response);
        }
    }
}
