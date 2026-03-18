using Common.GameData.Tables;
using Efcore.Repositories;
using Common.Helpers;
using Common.Web;
using Shared.Constants;
using Shared.Types;

namespace WebServer.Services;

public class UsersService
{
    private readonly ILogger<UsersService> mLogger;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly IUsersRepository mUsersRepository;
    private readonly IKeyValueStore mKeyValueStore;
    private readonly ISecurityHelper mSecurityHelper;

    public UsersService(
        ILogger<UsersService> logger,
        IHttpContextAccessor httpContextAccessor,
        IUsersRepository usersRepository,
        IKeyValueStore keyValueStore,
        ISecurityHelper securityHelper)
    {
        mLogger = logger;
        mHttpContextAccessor = httpContextAccessor;
        mUsersRepository = usersRepository;
        mKeyValueStore = keyValueStore;
        mSecurityHelper = securityHelper;
    }

    public async Task<ApiResponse> JoinAsync(
        string email, string password, string nickname)
    {
        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(nickname))
            return ApiResponse
                .From(EResponseResult.EmptyRequiredField);

        if (!ValidationHelper.IsValidEmail(email))
            return ApiResponse
                .From(EResponseResult.EmailAlphabetNumberOnly);
        
        if (!ValidationHelper.IsValidNickname(nickname))
            return ApiResponse
                .From(EResponseResult.NicknameAlphabetKoreanNumberOnly);

        if (email.Length is 
                < AppConstant.EAMIL_MIN_LENGTH or > AppConstant.EAMIL_MAX_LENGTH)
            return ApiResponse
                .From(EResponseResult.InvalidEmailLength);

        if (nickname.Length is 
                < AppConstant.NICKNAME_MIN_LENGTH or > AppConstant.NICKNAME_MAX_LENGTH)
            return ApiResponse
                .From(EResponseResult.InvalidNicknameLength);
        
        var bExistsUser = await mUsersRepository.ExistsUserAsync(email);
        
        if (bExistsUser)
            return ApiResponse
                .From(EResponseResult.EmailAlreadyExists);
        
        if (BannedWordTable.ContainsBannedWord(email))
            return ApiResponse
                .From(EResponseResult.ForbiddenEmail);

        if (BannedWordTable.ContainsBannedWord(nickname))
            return ApiResponse
                .From(EResponseResult.ForbiddenNickname);

        var hashPw = mSecurityHelper.ComputeSha512Hash(password);
        await mUsersRepository.AddAsync(email, nickname, hashPw);
        
        return ApiResponse
            .From(EResponseResult.Success);
    }

    public async Task<ApiResponse> LoginAsync(
        string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
            return ApiResponse.From(EResponseResult.EmptyRequiredField);

        if (!ValidationHelper.IsValidEmail(email))
            return ApiResponse.From(EResponseResult.EmailAlphabetNumberOnly);
        
        if (email.Length is 
            < AppConstant.EAMIL_MIN_LENGTH or > AppConstant.EAMIL_MAX_LENGTH)
            return ApiResponse.From(EResponseResult.InvalidEmailLength);
        
        var user = await mUsersRepository.FindUserByEmailAsync(email);

        if (user == null)
            return ApiResponse
                .From(EResponseResult.NoData);

        if (!mSecurityHelper.VerifySha512Hash(password, user.Pw))
            return ApiResponse.From(EResponseResult.NotMatchPw);
        
        await mKeyValueStore.AddSessionInfoAsync(
            $"{user.UserId}", 
            mHttpContextAccessor.HttpContext!.Session.Id);

        mHttpContextAccessor.SetUserSession(
            $"{user.UserId}", $"{user.Nickname}");
        
        return ApiResponse.From(EResponseResult.Success);
    }
}