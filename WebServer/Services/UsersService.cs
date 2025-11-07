using Microsoft.EntityFrameworkCore;
using SharedLibrary.Common;
using SharedLibrary.Efcore;
using SharedLibrary.Models;
using SharedLibrary.Protocol.Common;
using SharedLibrary.Redis;
using WebServer.Common;
using WebServer.Helpers;

namespace WebServer.Services;

public class UsersService
{
    private readonly ILogger<UsersService> mLogger;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly IDbContextFactory<AppDbContext> mDbContextFactory;
    private readonly IRedisClient mRedisClient;

    #region TODO DB에서 관리하는 부분 제작하기, 금지 닉네임과 아이디 설정
    private HashSet<string> bannedEmails = ["admin"];
    private HashSet<string> bannedNicknames = ["admin"];
    #endregion

    public UsersService(
        ILogger<UsersService> logger,
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<AppDbContext> dbContextFactory,
        IRedisClient redisClient)
    {
        mLogger = logger;
        mHttpContextAccessor = httpContextAccessor;
        mDbContextFactory = dbContextFactory;
        mRedisClient = redisClient;
    }

    public async Task<ApiResponse> Join(string email, string password, string nickname, AppEnum.ELanguage language)
    {
        if (!ValidationHelper.IsValidEmail(email))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmailAlphabetNumberOnly, language));
        
        if (!ValidationHelper.IsValidNickname(nickname))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.NicknameAlphabetKoreanNumberOnly, language));

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
        
        await using var db = await mDbContextFactory.CreateDbContextAsync();
        
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

        var hashPw = HashHelper.ComputeSha512Hash(password);
        
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

    public async Task<ApiResponse> CheckPassword(string email, string password, AppEnum.ELanguage language)
    {
        if (!ValidationHelper.IsValidEmail(email))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmailAlphabetNumberOnly, language));
        
        await using var db = await mDbContextFactory.CreateDbContextAsync(); 
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.NoData, language));

        if (!HashHelper.VerifySha512Hash(password, user.Pw))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.NotMatchPw, language));
        
        var httpContext = mHttpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new InvalidOperationException(Common.ExceptionMessage.EMPTY_HTTP_CONTEXT);
        
        await mRedisClient.AddSessionInfoAsync($"{user.UserId}", httpContext.Session.Id);
        
        httpContext.Session.SetString("userId", $"{user.UserId}");
        httpContext.Session.SetString("nickname", $"{user.Nickname}");
        httpContext.Session.SetString("language", $"{language}");

        return new ApiResponse(
            ResponseStatus.FromResponseStatus(
                EResponseStatus.Success, language));
    }
}