using WebServer.Common;
using WebServer.DAOs;
using WebServer.Helpers;
using WebServer.Infrastructure;

namespace WebServer.Services;

public class UsersService
{
    private readonly ILogger<UsersService> mLogger;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly UsersDao mUsersDao;
    private readonly RedisClient mRedisClient;
    private readonly HttpContext mHttpContext;

    #region TODO DB에서 관리하는 부분 제작하기, 금지 닉네임과 아이디 설정
    HashSet<string> bannedEmails = [];
    HashSet<string> bannedNicknames = [];
    #endregion

    public UsersService(
        ILogger<UsersService> logger,
        IHttpContextAccessor httpContextAccessor,
        UsersDao usersDao, 
        RedisClient redisClient)
    {
        mLogger = logger;
        mHttpContextAccessor = httpContextAccessor;
        mUsersDao = usersDao;
        mRedisClient = redisClient;
        
        if (httpContextAccessor.HttpContext == null)
        {
            mLogger.LogCritical("HttpContext is missing required configuration for session-based authentication.");
            Environment.Exit(1);
        }
        
        mHttpContext = httpContextAccessor.HttpContext;

        #region 금지 닉네임과 아이디 설정
        bannedEmails.Add("admin");
        bannedNicknames.Add("admin");
        #endregion
    }

    public async Task<ApiResponse> Join(string email, string pw, string nickname)
    {
        if (!ValidationHelper.IsValidEmail(email))
            return new ApiResponse(ResponseStatus.emailAlphabetNumericOnly);

        if (email.Length is 
                < AppConstant.EAMIL_MIN_LENGTH or > AppConstant.EAMIL_MAX_LENGTH)
            return new ApiResponse(ResponseStatus.invalidEmailLength);

        if (nickname.Length is 
                < AppConstant.NICKNAME_MIN_LENGTH or > AppConstant.NICKNAME_MAX_LENGTH)
            return new ApiResponse(ResponseStatus.invalidNicknameLength);
        
        if (await mUsersDao.ExistsByEmail(email))
            return new ApiResponse(ResponseStatus.emailAlreadyExists);

        var containsForbiddenWord =
            bannedEmails.Any(word => email.Contains(word, StringComparison.OrdinalIgnoreCase));

        if (containsForbiddenWord)
            return new ApiResponse(ResponseStatus.forbiddenEmail);

        containsForbiddenWord =
           bannedNicknames.Any(word => nickname.Contains(word, StringComparison.OrdinalIgnoreCase));

        if (containsForbiddenWord)
            return new ApiResponse(ResponseStatus.forbiddenNickname);

        var hashPw = HashHelper.ComputeSha512Hash(pw);

        if (!await mUsersDao.Save(nickname, email, hashPw))
        {
            mLogger.LogError("Database error occurred while saving user information.");
            return new ApiResponse(ResponseStatus.dbError);
        }

        var user = await mUsersDao.FindByEmail(email);
        if (user == null)
        {
            mLogger.LogError("Database error occurred while finding user information.");
            return new ApiResponse(ResponseStatus.dbError); 
        }
        
        return new ApiResponse(ResponseStatus.success);
    }

    public async Task<ApiResponse> CheckPassword(string email, string pw)
    {
        var user = await mUsersDao.FindByEmail(email);

        if (user == null)
            return new ApiResponse(ResponseStatus.emptyUser);

        if (!HashHelper.VerifySha512Hash(pw, user.Pw))
            return new ApiResponse(ResponseStatus.notMatchPw);
        
        mHttpContext.Session.SetString("userId", $"{user.UserId}");
        mHttpContext.Session.SetString("nickname", $"{user.Nickname}");

        return new ApiResponse(ResponseStatus.success);
    }
}