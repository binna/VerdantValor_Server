using Common.GameData.Tables;
using Efcore.Repositories;
using Common.Helpers;
using Common.KeyValueStore;
using Common.Types;
using Protocol.Web.Dtos;
using Shared.Types;
using WebServer.options;

namespace WebServer.Services;

public class GameUserService
{
    private readonly ILogger<GameUserService> mLogger;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly IGameUserRepository mGameUserRepository;
    private readonly ISessionKeyValueStore mSessionKeyValueStore;
    private readonly ISecurityHelper mSecurityHelper;
    private readonly ServerOption mServerOption;

    public GameUserService(
        ILogger<GameUserService> logger,
        IHttpContextAccessor httpContextAccessor,
        IGameUserRepository gameUserRepository,
        ISessionKeyValueStore sessionKeyValueStore,
        ISecurityHelper securityHelper,
        ServerOption serverOption)
    {
        mLogger = logger;
        mHttpContextAccessor = httpContextAccessor;
        mGameUserRepository = gameUserRepository;
        mSessionKeyValueStore = sessionKeyValueStore;
        mSecurityHelper = securityHelper;
        mServerOption = serverOption;
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

    public async Task<(EResponseResult, AuthRes)> LoginAsync(string email, string password, string deviceId)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(deviceId))
            return (EResponseResult.EmptyRequiredField, new AuthRes());
        
        var user = await mGameUserRepository.FindByEmailAsync(email);
        if (user == null)
            return (EResponseResult.NoData, new AuthRes());

        if (!mSecurityHelper.VerifySha512Hash(password, user.Pw))
            return (EResponseResult.PasswordMismatch, new AuthRes());
        
        var sessionId = $"{mServerOption.Name}_{Guid.NewGuid():N}";
        await mSessionKeyValueStore.AddUserSessionInfoAsync(
            $"{user.UserId}", 
            new UserSessionInfo
            {
                SessionId = sessionId,
                DeviceId = deviceId
            });

        mHttpContextAccessor.SetUserSession(
            $"{user.UserId}", $"{user.Nickname}");
        
        return (EResponseResult.Success, new AuthRes { SessionId = sessionId });
    }
    
    public async Task<bool> SendHeartbeatAsync(string userId)
    {
        return await mSessionKeyValueStore.ExtendUserSessionInfoAsync(userId);
    }

    // TODO 체팅 어느 서버에 배정됬는지,,, 연결하는 부분
    
    // TODO 로그아웃
}