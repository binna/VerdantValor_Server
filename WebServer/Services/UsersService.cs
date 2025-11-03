using Microsoft.EntityFrameworkCore;
using SharedLibrary.Common;
using SharedLibrary.DAOs;
using SharedLibrary.Database.EFCore;
using SharedLibrary.Database.Redis;
using WebServer.Common;
using WebServer.Helpers;

namespace WebServer.Services;

public class UsersService
{
    private readonly ILogger<UsersService> mLogger;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly IDbContextFactory<AppDbContext> mDbContextFactory;    
    private readonly UsersDao mUsersDao;

    #region TODO DB에서 관리하는 부분 제작하기, 금지 닉네임과 아이디 설정
    HashSet<string> bannedEmails = [];
    HashSet<string> bannedNicknames = [];
    #endregion

    public UsersService(
        ILogger<UsersService> logger,
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<AppDbContext> dbContextFactory,
        UsersDao usersDao)
    {
        mLogger = logger;
        mHttpContextAccessor = httpContextAccessor;
        mDbContextFactory = dbContextFactory;
        mUsersDao = usersDao;

        #region 금지 닉네임과 아이디 설정
        bannedEmails.Add("admin");
        bannedNicknames.Add("admin");
        #endregion
    }

    public async Task<ApiResponse> Join(string email, string pw, string nickname, AppConstant.ELanguage language)
    {
        if (!ValidationHelper.IsValidEmail(email))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmailAlphabetNumericOnly, language));

        if (email.Length is 
                < AppConstant.EAMIL_MIN_LENGTH or > AppConstant.EAMIL_MAX_LENGTH)
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidEmailLength, language));

        if (nickname.Length is 
                < AppConstant.NICKNAME_MIN_LENGTH or > AppConstant.NICKNAME_MAX_LENGTH)
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidNicknameLength, language));
        
        if (await mUsersDao.ExistsByEmail(email))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmailAlreadyExists, language));

        var containsForbiddenWord =
            bannedEmails.Any(word => email.Contains(word, StringComparison.OrdinalIgnoreCase));

        if (containsForbiddenWord)
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.ForbiddenEmail, language));

        containsForbiddenWord =
           bannedNicknames.Any(word => nickname.Contains(word, StringComparison.OrdinalIgnoreCase));

        if (containsForbiddenWord)
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.ForbiddenNickname, language));

        var hashPw = HashHelper.ComputeSha512Hash(pw);

        if (!await mUsersDao.Save(nickname, email, hashPw))
        {
            mLogger.LogError("Database error occurred while saving user information.");
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.DbError, language));
        }
        
        return new ApiResponse(
            ResponseStatus.FromResponseStatus(
                EResponseStatus.Success, language));
    }

    public async Task<ApiResponse> CheckPassword(string email, string pw, AppConstant.ELanguage language)
    {
        await using var db = await mDbContextFactory.CreateDbContextAsync(); 
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyUser, language));

        if (!HashHelper.VerifySha512Hash(pw, user.Pw))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.NotMatchPw, language));
        
        var httpContext = mHttpContextAccessor.HttpContext;
        if (httpContext == null)
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyAuth, language));
        
        Console.WriteLine("login >>>>>>>>>>>>> " + httpContext.Session.Id);

        await RedisClient.Instance.AddSessionInfoAsync($"{user.UserId}", httpContext.Session.Id);
        
        httpContext.Session.SetString("userId", $"{user.UserId}");
        httpContext.Session.SetString("nickname", $"{user.Nickname}");
        httpContext.Session.SetString("language", $"{language}");

        return new ApiResponse(
            ResponseStatus.FromResponseStatus(
                EResponseStatus.Success, language));
    }
}