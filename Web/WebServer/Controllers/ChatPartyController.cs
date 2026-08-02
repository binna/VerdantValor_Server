using Common.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Protocol.Web.Dtos;
using Shared.Constants;
using Shared.Types;
using WebServer.Services;

namespace WebServer.Controllers;

[Route($"{AppConstant.WEB_SERVER_API_BASE}/[controller]")]
[ApiController]
public class ChatPartyController : Controller
{
    private readonly ILogger<ChatPartyController> mLogger;
    private readonly ChatPartyService mChatPartyService;

    public ChatPartyController(
        ILogger<ChatPartyController> logger,
        ChatPartyService chatPartyService)
    {
        mLogger = logger;
        mChatPartyService = chatPartyService;
    }

    [HttpPost("ChatParty")]
    [Authorize(Policy = "SessionPolicy")]
    public async Task<ApiResponse> ChatParty([FromBody] ChatPartyReq request)
    {
        if (!Enum.TryParse<EChatPartyType>(request.ChatPartyType, out var chatPartyType))
            return ApiResponse.From(EResponseResult.InvalidInput);
        
        if (!ulong.TryParse(this.GetUserId(), out var userId))
            return ApiResponse.From(EResponseResult.InvalidUserId);

        switch (chatPartyType)
        {
            case EChatPartyType.Create:
            {
                var code = await mChatPartyService.CreateAsync(userId, request.Name);
                return ApiResponse.From(code);
            }
            case EChatPartyType.Delete:
            {
                var code = await mChatPartyService.DeleteAsync(userId);
                return ApiResponse.From(code);
            }
            case EChatPartyType.Invite:
            {
                var code = await mChatPartyService.InviteAsync(userId, request.InviteUserId);
                return ApiResponse.From(code);
            }
            default:
                mLogger.LogError("Invalid Type {Type}", chatPartyType);
                return ApiResponse.From(EResponseResult.InvalidInput);
        }
    }
    
    [HttpPost("AcceptInvite")]
    [Authorize(Policy = "SessionPolicy")]
    public async Task<ApiResponse> AcceptInvite([FromBody] AcceptInviteReq request)
    {
        if (!ulong.TryParse(this.GetUserId(), out var userId))
            return ApiResponse.From(EResponseResult.InvalidUserId);

        var code = await mChatPartyService.AcceptInviteAsync(request.PartyId, userId);
        return ApiResponse.From(code);
    }
}
