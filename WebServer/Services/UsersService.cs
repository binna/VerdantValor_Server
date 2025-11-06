using Microsoft.EntityFrameworkCore;
using SharedLibrary.Common;
using SharedLibrary.DAOs;
using SharedLibrary.Database.EFCore;
using SharedLibrary.Database.Redis;
using SharedLibrary.Models;
using WebServer.Common;
using WebServer.Helpers;

namespace WebServer.Services;

public class UsersService
{
    private readonly ILogger<UsersService> mLogger;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly IDbContextFactory<AppDbContext> mDbContextFactory;
    private readonly RedisClient mRedisClient;

    #region TODO DB에서 관리하는 부분 제작하기, 금지 닉네임과 아이디 설정
    private HashSet<string> bannedEmails = ["admin"];
    private HashSet<string> bannedNicknames = ["admin"];
    #endregion

    public UsersService(
        ILogger<UsersService> logger,
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<AppDbContext> dbContextFactory,
        RedisClient redisClient)
    {
        mLogger = logger;
        mHttpContextAccessor = httpContextAccessor;
        mDbContextFactory = dbContextFactory;
        mRedisClient = redisClient;
    }

    public async Task<ApiResponse> Join(string email, string pw, string nickname, AppConstant.ELanguage language)
    {
        await using var db = await mDbContextFactory.CreateDbContextAsync(); 
       
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
        
        var bExistsUser = await db.Users
            .AnyAsync(u => u.Email == email);
        
        if (bExistsUser)
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
        
        await db.Users.AddAsync(new Users(nickname, email, hashPw));
        var result = await db.SaveChangesAsync();

        if (result > 0)
        {
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.Success, language));
        }
        
        mLogger.LogError("Database error occurred while saving user information. {@context}",
            new { nickname, email, hashPw });
            
        return new ApiResponse(
            ResponseStatus.FromResponseStatus(
                EResponseStatus.DbError, language));
    }

    public async Task<ApiResponse> CheckPassword(string email, string pw, AppConstant.ELanguage language)
    {
        await using var db = await mDbContextFactory.CreateDbContextAsync(); 
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.NoData, language));

        if (!HashHelper.VerifySha512Hash(pw, user.Pw))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.NotMatchPw, language));
        
        var httpContext = mHttpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new InvalidOperationException(ExceptionMessage.EMPTY_HTTP_CONTEXT);
        
        await mRedisClient.AddSessionInfoAsync($"{user.UserId}", httpContext.Session.Id);
        
        httpContext.Session.SetString("userId", $"{user.UserId}");
        httpContext.Session.SetString("nickname", $"{user.Nickname}");
        httpContext.Session.SetString("language", $"{language}");

        return new ApiResponse(
            ResponseStatus.FromResponseStatus(
                EResponseStatus.Success, language));
    }
}