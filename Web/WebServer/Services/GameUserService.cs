using Common.GameData.Tables;
using Efcore.Repositories;
using Common.Helpers;
using Common.KeyValueStore;
using Common.Types;
using Shared.Types;

namespace WebServer.Services;

public class GameUserService
{
    private readonly ILogger<GameUserService> mLogger;
    private readonly IHttpContextAccessor mHttpContextAccessor;
    private readonly IGameUserRepository mGameUserRepository;
    private readonly ISessionKeyValueStore mSessionKeyValueStore;
    private readonly ISecurityHelper mSecurityHelper;

    public GameUserService(
        ILogger<GameUserService> logger,
        IHttpContextAccessor httpContextAccessor,
        IGameUserRepository gameUserRepository,
        ISessionKeyValueStore sessionKeyValueStore,
        ISecurityHelper securityHelper)
    {
        mLogger = logger;
        mHttpContextAccessor = httpContextAccessor;
        mGameUserRepository = gameUserRepository;
        mSessionKeyValueStore = sessionKeyValueStore;
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

    public async Task<EResponseResult> LoginAsync(string email, string password, string deviceId)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(deviceId))
            return EResponseResult.EmptyRequiredField;
        
        var user = await mGameUserRepository.FindByEmailAsync(email);
        if (user == null)
            return EResponseResult.NoData;

        if (!mSecurityHelper.VerifySha512Hash(password, user.Pw))
            return EResponseResult.PasswordMismatch;
        
        // TODO 이건 추가적인 UserID에 대한 세션 저장
        //  이걸 세션구조의 문서처럼 저장하는게 맞음 -> 완료
        //  그리고 세션 번호는 서버 전체가 공유해야하는 거고 Config에서 Common으로 static 상수로 빼야할 듯
        await mSessionKeyValueStore.AddUserSessionInfoAsync(
            $"{user.UserId}", 
            new UserSessionInfo
            {
                SessionId = mHttpContextAccessor.HttpContext!.Session.Id,
                DeviceId = deviceId
            });

        mHttpContextAccessor.SetUserSession(
            $"{user.UserId}", $"{user.Nickname}");
        
        return EResponseResult.Success;
    }
    
    // TODO 체팅 어느 서버에 배정됬는지,,, 연결하는 부분
    
    // TODO 로그아웃
}