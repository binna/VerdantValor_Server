using SharedLibrary.Common;
using SharedLibrary.Efcore.Repository;
using SharedLibrary.Efcore.Transaction;
using SharedLibrary.Helpers;
using SharedLibrary.Protocol.Common;
using SharedLibrary.Redis;

namespace WebServer.Services;

public class UsersService
{
    private readonly ILogger<UsersService> mLogger;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly IUsersRepository mUsersRepository;
    private readonly IUsersServiceTransaction mUsersTransaction;
    private readonly IRedisClient mRedisClient;

    #region TODO DB에서 관리하는 부분 제작하기, 금지 닉네임과 아이디 설정
    private HashSet<string> bannedEmails = ["admin"];
    private HashSet<string> bannedNicknames = ["admin"];
    #endregion

    public UsersService(
        ILogger<UsersService> logger,
        IHttpContextAccessor httpContextAccessor,
        IUsersRepository usersRepository,
        IUsersServiceTransaction usersTransaction,
        IRedisClient redisClient)
    {
        mLogger = logger;
        mHttpContextAccessor = httpContextAccessor;
        mUsersRepository = usersRepository;
        mUsersTransaction = usersTransaction;
        mRedisClient = redisClient;
    }

    public async Task<ApiResponse> Join(
        string email, string password, string nickname, 
        AppEnum.ELanguage language = AppEnum.ELanguage.En)
    {
        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(nickname))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyRequiredField, language));

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
        
        var bExistsUser = await mUsersRepository.ExistsUserAsync(email);
        
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
        var result = await mUsersTransaction.CreateUserAsync(email, nickname, hashPw);

        if (result > 0)
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.Success, language));
        
        mLogger.LogError("Database error occurred while saving user information. {@context}",
            new { email, nickname, hashPw });
            
        return new ApiResponse(
            ResponseStatus.FromResponseStatus(
                EResponseStatus.DbError, language));
    }

    public async Task<ApiResponse> Login(
        string email, string password, 
        AppEnum.ELanguage language = AppEnum.ELanguage.En)
    {
        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmptyRequiredField, language));

        if (!ValidationHelper.IsValidEmail(email))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.EmailAlphabetNumberOnly, language));
        
        if (email.Length is 
            < AppConstant.EAMIL_MIN_LENGTH or > AppConstant.EAMIL_MAX_LENGTH)
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidEmailLength, language));
        
        var user = await mUsersRepository.FindUserByEmailAsync(email);

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