using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Common;
using SharedLibrary.Protocol.Common;
using SharedLibrary.Protocol.DTOs;
using WebServer.Services;

namespace WebServer.Controllers;

[Route($"{AppConstant.WEB_SERVER_API_BASE}/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly UsersService mUsersService;

    public UsersController(UsersService usersService)
    {
        mUsersService = usersService;
    }

    [HttpPost("Auth")]
#if DEVELOPMENT
    public async Task<ApiResponse> AuthAsync([FromBody] AuthReq request)
#elif LIVE
    public async Task<ApiResponse> AuthAsync([FromBody] EncryptReq encryptReq)
    {
        AuthReq? request;
        using (var aesCcm = new AesCcm(AppReadonly.REQ_ENCRYPT_KEY))
        {
            var nonceBytes = Convert.FromBase64String(encryptReq.Nonce);
            var dataBytes = Convert.FromBase64String(encryptReq.Data);
            var tagBytes = Convert.FromBase64String(encryptReq.Tag);
            var plainBytes = new byte[nonceBytes.Length];
        
            aesCcm.Decrypt(nonceBytes, dataBytes, tagBytes, plainBytes);

            var plaintext = Encoding.UTF8.GetString(plainBytes);
        
            request = JsonSerializer.Deserialize<AuthReq>(plaintext);
            
            if (request == null)
                return new ApiResponse<RankRes>(
                    ResponseStatus.FromResponseStatus(
                        EResponseStatus.FailDecrypt, AppEnum.ELanguage.Ko)); 
        }
#endif
        if (!Enum.TryParse<AppEnum.ELanguage>(request.Language, out var language))
            language = AppEnum.ELanguage.En;

        if (!Enum.TryParse<AppEnum.EAuthType>(request.AuthType, out var authType))
            return new ApiResponse(
                ResponseStatus.FromResponseStatus(
                    EResponseStatus.InvalidInput, language));

        switch (authType)
        {
            case AppEnum.EAuthType.Join:
                return await mUsersService.Join(
                    request.Email, request.Pw, request.Nickname, language);
            case AppEnum.EAuthType.Login:
                return await mUsersService.Login(
                    request.Email, request.Pw, language);
            default:
                return new ApiResponse(ResponseStatus.FromResponseStatus(
                    EResponseStatus.Success, language));
        }
    }
}