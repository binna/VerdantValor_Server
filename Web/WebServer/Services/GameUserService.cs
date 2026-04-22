using Common.GameData.Tables;
using Efcore.Repositories;
using Common.Helpers;
using Common.Web;
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

    public async Task<EResponseResult> JoinAsync(string email, string password, string nickname)
    {
        var responseResult = ValidationHelper.IsValidEmail(email);
        if (responseResult != EResponseResult.Success)
            return responseResult;
        
        responseResult = ValidationHelper.IsValidPassword(password);
        if (responseResult != EResponseResult.Success)
            return responseResult;
        
        responseResult = ValidationHelper.IsValidNickname(nickname);
        if (responseResult != EResponseResult.Success)
            return responseResult;
        
        if (BannedWordTable.ContainsBannedWord(email))
            return EResponseResult.ForbiddenEmail;
        
        if (BannedWordTable.ContainsBannedWord(nickname))
            return EResponseResult.ForbiddenNickname;
        
        var bExistsUser = await mGameUserRepository.ExistsAsync(email);
        if (bExistsUser)
            return EResponseResult.EmailAlreadyExists;
        
        // TODO 현재 진행중인데 같은 email로 요청이 들어온다면,,,,
        //  디비에서 유효성 검사하긴 하지만, 그래도 서버 내에서 막는 로직, 레디스 이용 예정

        var hashPw = mSecurityHelper.ComputeSha512Hash(password);
        await mGameUserRepository.AddAsync(email, nickname, hashPw);
        
        return EResponseResult.Success;
    }

    public async Task<EResponseResult> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return EResponseResult.EmptyRequiredField;
        
        var user = await mGameUserRepository.FindByEmailAsync(email);
        if (user == null)
            return EResponseResult.NoData;

        if (!mSecurityHelper.VerifySha512Hash(password, user.Pw))
            return EResponseResult.NotMatchPw;
        
        await mKeyValueStore.AddSessionInfoAsync(
            $"{user.UserId}", mHttpContextAccessor.HttpContext!.Session.Id);

        mHttpContextAccessor.SetUserSession(
            $"{user.UserId}", $"{user.Nickname}");
        
        return EResponseResult.Success;
    }
}