using Efcore.Repositories;
using Common.Helpers;
using Common.Web;
using Shared.Constants;
using Shared.Types;

namespace WebServer.Services;

public class GameUserService
{
    private readonly ILogger<GameUserService> mLogger;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly IGameUserRepository mGameUserRepository;
    private readonly IKeyValueStore mKeyValueStore;
    private readonly ISecurityHelper mSecurityHelper;

    public GameUserService(
        ILogger<GameUserService> logger,
        IHttpContextAccessor httpContextAccessor,
        IGameUserRepository gameUserRepository,
        IKeyValueStore keyValueStore,
        ISecurityHelper securityHelper)
    {
        mLogger = logger;
        mHttpContextAccessor = httpContextAccessor;
        mGameUserRepository = gameUserRepository;
        mKeyValueStore = keyValueStore;
        mSecurityHelper = securityHelper;
    }

    public async Task<ApiResponse> JoinAsync(string email, string password, string nickname)
    {
        var responseResult = ValidationHelper.IsValidEmail(email, ValidationHelper.EValidationFlags.CheckBannedWord);
        if (responseResult != EResponseResult.Success)
            return ApiResponse.From(responseResult);
        
        responseResult = ValidationHelper.IsValidPassWord(password);
        if (responseResult != EResponseResult.Success)
            return ApiResponse.From(responseResult);
        
        responseResult = ValidationHelper.IsValidNickname(nickname, ValidationHelper.EValidationFlags.CheckBannedWord);
        if (responseResult != EResponseResult.Success)
            return ApiResponse.From(responseResult);
        
        var bExistsUser = await mGameUserRepository.ExistsAsync(email);
        
        if (bExistsUser)
            return ApiResponse
                .From(EResponseResult.EmailAlreadyExists);
        
        // TODO 현재 진행중인데 같은 email로 요청이 들어온다면,,,,
        //  디비에서 유효성 검사하긴 하지만, 그래도 서버 내에서 막는 로직, 레디스 이용 예정

        var hashPw = mSecurityHelper.ComputeSha512Hash(password);
        await mGameUserRepository.AddAsync(email, nickname, hashPw);
        
        return ApiResponse.From(EResponseResult.Success);
    }

    public async Task<ApiResponse> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return ApiResponse.From(EResponseResult.EmptyRequiredField);

        var responseResult = ValidationHelper.IsValidEmail(email, ValidationHelper.EValidationFlags.None);

        if (responseResult != EResponseResult.Success)
            return ApiResponse.From(responseResult);
        
        if (email.Length is 
            < AppConstant.EAMIL_MIN_LENGTH or > AppConstant.EAMIL_MAX_LENGTH)
            return ApiResponse.From(EResponseResult.InvalidEmailLength);
        
        var user = await mGameUserRepository.FindByEmailAsync(email);

        if (user == null)
            return ApiResponse
                .From(EResponseResult.NoData);

        if (!mSecurityHelper.VerifySha512Hash(password, user.Pw))
            return ApiResponse.From(EResponseResult.NotMatchPw);
        
        await mKeyValueStore.AddSessionInfoAsync(
            $"{user.UserId}", mHttpContextAccessor.HttpContext!.Session.Id);

        mHttpContextAccessor.SetUserSession(
            $"{user.UserId}", $"{user.Nickname}");
        
        return ApiResponse.From(EResponseResult.Success);
    }
}