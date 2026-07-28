using Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Efcore.Repositories;

public class ChatPartyRepository : IChatPartyRepository
{
    private readonly IHttpContextAccessor mHttpContextAccessor;

    public ChatPartyRepository(IHttpContextAccessor httpContextAccessor)
    {
        mHttpContextAccessor = httpContextAccessor;
    }
    
    public async Task<bool> HasOwnerAsync(ulong ownerUserId)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        return await dbContext.ChatParty.AnyAsync(p => p.OwnerUserId == ownerUserId);
    }
    
    public async Task<bool> ExistsAsync(string partyId)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        return await dbContext.ChatParty.AnyAsync(p => p.PartyId == partyId);
    }
    
    public async Task<bool> IsOwnerAsync(string partyId, ulong ownerUserId)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        return await dbContext.ChatParty.AnyAsync(p => p.PartyId == partyId &&  p.OwnerUserId == ownerUserId);
    }

    public async Task AddAsync(ChatParty chatParty)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        await dbContext.ChatParty.AddAsync(chatParty);
    }
    
    public async Task InviteAddAsync(string partyId, ulong invitedUserId)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        await dbContext.ChatPartyInvitation.AddAsync(new ChatPartyInvitation(partyId, invitedUserId));
    }

    public async Task<bool> MemberAddAsync(string partyId, ulong invitedUserId)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        
        var invitation = await dbContext.ChatPartyInvitation
            .FirstOrDefaultAsync(x => x.PartyId == partyId && x.UserId == invitedUserId);

        if (invitation is null)
            return false;
        
        dbContext.ChatPartyInvitation.Remove(invitation);
        await dbContext.ChatPartyMember.AddAsync(new ChatPartyMember(partyId, invitedUserId));
        
        return true;
    }
    
    public async Task<bool> DeleteAsync(ulong ownerUserId)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();

        var party = await dbContext.ChatParty
            .FirstOrDefaultAsync(x => x.OwnerUserId == ownerUserId);

        if (party is null)
            return false;

        var members = await dbContext.ChatPartyMember
            .Where(x => x.PartyId == party.PartyId)
            .ToListAsync();

        var invitations = await dbContext.ChatPartyInvitation
            .Where(x => x.PartyId == party.PartyId)
            .ToListAsync();

        dbContext.ChatPartyMember.RemoveRange(members);
        dbContext.ChatPartyInvitation.RemoveRange(invitations);
        dbContext.ChatParty.Remove(party);

        return true;
    }
}
