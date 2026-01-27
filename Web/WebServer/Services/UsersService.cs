using Common.Base;
using Efcore.Repository;
using Common.Helpers;
using VerdantValorShared.Common.Web;
using Redis;

namespace WebServer.Services;

public class UsersService
{
    private readonly ILogger<UsersService> mLogger;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly IUsersRepository mUsersRepository;
    private readonly IRedisClient mRedisClient;

    #region TODO DB에서 관리하는 부분 제작하기, 금지 닉네임과 아이디 설정
    private HashSet<string> bannedEmails = ["admin"];
    private HashSet<string> bannedNicknames = ["admin"];
    #endregion

    public UsersService(
        ILogger<UsersService> logger,
        IHttpContextAccessor httpContextAccessor,
        IUsersRepository usersRepository,
        IRedisClient redisClient)
    {
        mLogger = logger;
        mHttpContextAccessor = httpContextAccessor;
        mUsersRepository = usersRepository;
        mRedisClient = redisClient;
    }

    public async Task<ApiResponse> JoinAsync(
        string email, string password, string nickname, 
        AppEnum.ELanguage language = AppEnum.ELanguage.En)
    {
        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(nickname))
            return ApiResponse
                .From(AppEnum.EResponseStatus.EmptyRequiredField, language);

        if (!ValidationHelper.IsValidEmail(email))
            return ApiResponse
                .From(AppEnum.EResponseStatus.EmailAlphabetNumberOnly, language);
        
        if (!ValidationHelper.IsValidNickname(nickname))
            return ApiResponse
                .From(AppEnum.EResponseStatus.NicknameAlphabetKoreanNumberOnly, language);

        if (email.Length is 
                < AppConstant.EAMIL_MIN_LENGTH or > AppConstant.EAMIL_MAX_LENGTH)
            return ApiResponse
                .From(AppEnum.EResponseStatus.InvalidEmailLength, language);

        if (nickname.Length is 
                < AppConstant.NICKNAME_MIN_LENGTH or > AppConstant.NICKNAME_MAX_LENGTH)
            return ApiResponse
                .From(AppEnum.EResponseStatus.InvalidNicknameLength, language);
        
        var bExistsUser = await mUsersRepository.ExistsUserAsync(email);
        
        if (bExistsUser)
            return ApiResponse
                .From(AppEnum.EResponseStatus.EmailAlreadyExists, language);

        var containsForbiddenWord =
            bannedEmails.Any(word => email.Contains(word, StringComparison.OrdinalIgnoreCase));

        if (containsForbiddenWord)
            return ApiResponse
                .From(AppEnum.EResponseStatus.ForbiddenEmail, language);

        containsForbiddenWord =
           bannedNicknames.Any(word => nickname.Contains(word, StringComparison.OrdinalIgnoreCase));

        if (containsForbiddenWord)
            return ApiResponse
                .From(AppEnum.EResponseStatus.ForbiddenNickname, language);

        var hashPw = SecurityHelper.ComputeSha512Hash(password);
        await mUsersRepository.AddAsync(email, nickname, hashPw);
        
        return ApiResponse
            .From(AppEnum.EResponseStatus.Success, language);
    }

    public async Task<ApiResponse> LoginAsync(
        string email, string password, 
        AppEnum.ELanguage language = AppEnum.ELanguage.En)
    {
        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
            return ApiResponse
                .From(AppEnum.EResponseStatus.EmptyRequiredField, language);

        if (!ValidationHelper.IsValidEmail(email))
            return ApiResponse
                .From(AppEnum.EResponseStatus.EmailAlphabetNumberOnly, language);
        
        if (email.Length is 
            < AppConstant.EAMIL_MIN_LENGTH or > AppConstant.EAMIL_MAX_LENGTH)
            return ApiResponse
                .From(AppEnum.EResponseStatus.InvalidEmailLength, language);
        
        var user = await mUsersRepository.FindUserByEmailAsync(email);

        if (user == null)
            return ApiResponse
                .From(AppEnum.EResponseStatus.NoData, language);

        if (!SecurityHelper.VerifySha512Hash(password, user.Pw))
            return ApiResponse
                .From(AppEnum.EResponseStatus.NotMatchPw, language);
        
        await mRedisClient.AddSessionInfoAsync(
            $"{user.UserId}", 
            mHttpContextAccessor.HttpContext!.Session.Id);

        mHttpContextAccessor.SetUserSession(
            $"{user.UserId}",
            $"{user.Nickname}",
            $"{language}");
        
        return ApiResponse
            .From(AppEnum.EResponseStatus.Success, language);
    }
}