using Common.Models;
using Efcore.Repositories;
using Shared.Types;

namespace WebServer.Services;

public class ChatPartyService
{
    private readonly ILogger<ChatPartyService> mLogger;
    private readonly IChatPartyRepository mChatPartyRepository;
    
    const int RETRY_COUNT = 3;
    
    public ChatPartyService(
        ILogger<ChatPartyService> logger,
        IChatPartyRepository chatPartyRepository)
    {
        mLogger = logger;
        mChatPartyRepository = chatPartyRepository;
    }

    public async Task<EResponseResult> CreateAsync(ulong ownerUserId, string partyName)
    {
        var bHasPartyByOwnerUserId = await mChatPartyRepository.HasOwnerAsync(ownerUserId);
        if (bHasPartyByOwnerUserId)
            return EResponseResult.AlreadyHasParty;

        for (var retry = 0; retry < RETRY_COUNT; retry++)
        {
            var party = new ChatParty(partyName, ownerUserId);
            
            var bHasPartyByPartyId = await mChatPartyRepository.ExistsAsync(party.PartyId);
            if (bHasPartyByPartyId) continue;
            
            await mChatPartyRepository.AddAsync(party);
            return EResponseResult.Success;
        }

        return EResponseResult.UnexpectedError;
    }
    
    public async Task<EResponseResult> InviteAsync(ulong ownerUserId, ulong inviteUserId)
    {
        var partyId = await mChatPartyRepository.FindPartyIdByOwnerUserIdAsync(ownerUserId);

        if (partyId == null)
            return EResponseResult.NotOwner;

        await mChatPartyRepository.InviteAddAsync(partyId, inviteUserId);
        return EResponseResult.Success;
    }
    
    public async Task<EResponseResult> DeleteAsync(ulong userId)
    {
        var bDeleted = await mChatPartyRepository.DeleteAsync(userId);
        return !bDeleted ? EResponseResult.NotOwner : EResponseResult.Success;
    }
    
    public async Task<EResponseResult> AcceptInviteAsync(string partyId, ulong inviteUserId)
    {
        var bMemberAdd = await mChatPartyRepository.MemberAddAsync(partyId, inviteUserId);
        return !bMemberAdd ? EResponseResult.NotOwner : EResponseResult.Success;
    }
}
