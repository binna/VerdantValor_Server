using SharedLibrary.Models;
using WebServer.Common;
using WebServer.DAOs;
using WebServer.Helpers;
using WebServer.Infrastructure;

namespace WebServer.Services;

public class UsersService
{
    private readonly ILogger<UsersService> logger;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly UsersDao usersDao;
    private readonly RedisClient redisClient;

    // TODO DB에서 관리하는 부분 제작하기 =======
    public HashSet<string> bannedEmails = new();
    public HashSet<string> bannedNicknames = new();
    // ========================================

    public UsersService(
        ILogger<UsersService> logger,
        IHttpContextAccessor httpContextAccessor,
        UsersDao usersDao, 
        RedisClient redisClient)
    {
        this.logger = logger;
        this.httpContextAccessor = httpContextAccessor;
        this.usersDao = usersDao;
        this.redisClient = redisClient;

        // ==========================================
        bannedEmails.Add("admin");
        bannedNicknames.Add("admin");
        // ==========================================
    }

    public async Task<ApiResponse> Join(string email, string pw, string nickname)
    {
        if (await usersDao.ExistsByEmail(email))
            return new ApiResponse(ResponseStatus.emailAlreadyExists);

        bool containsForbiddenWord =
            bannedEmails.Any(word => email.Contains(word, StringComparison.OrdinalIgnoreCase));

        if (containsForbiddenWord)
            return new ApiResponse(ResponseStatus.forbiddenEmail);

        containsForbiddenWord =
           bannedNicknames.Any(word => nickname.Contains(word, StringComparison.OrdinalIgnoreCase));

        if (containsForbiddenWord)
            return new ApiResponse(ResponseStatus.forbiddenNickname);

        string hashpw = HashHelper.ComputeSHA512Hash(pw);

        if (!await usersDao.Save(nickname, email, hashpw))
        {
            logger.LogError("Database error occurred while saving user information.");
            return new ApiResponse(ResponseStatus.dbError);
        }

        var user = await usersDao.FindByEmail(email);
        if (user == null)
        {
            logger.LogError("Database error occurred while finding user information.");
            return new ApiResponse(ResponseStatus.dbError); 
        }
        
        return new ApiResponse(ResponseStatus.success);
    }

    public async Task<ApiResponse> CheckPassword(string email, string pw)
    {
        Users? user = await usersDao.FindByEmail(email);

        if (user == null)
            return new ApiResponse(ResponseStatus.emptyUser);

        if (!HashHelper.VerifySHA512Hash(pw, user.pw))
            return new ApiResponse(ResponseStatus.notMatchPw);
        
        httpContextAccessor.HttpContext!.Session.SetString("userId", $"{user.userId}");
        httpContextAccessor.HttpContext!.Session.SetString("nickname", $"{user.nickname}");

        Console.WriteLine(">>> " + httpContextAccessor.HttpContext.Session.GetString("userId"));
        Console.WriteLine(">>> " + httpContextAccessor.HttpContext.Session.GetString("nickname"));

        return new ApiResponse(ResponseStatus.success);
    }
}