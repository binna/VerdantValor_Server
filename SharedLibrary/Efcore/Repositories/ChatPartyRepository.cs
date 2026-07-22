using Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Efcore.Repositories;

public class ChatPartyRepository
{
    private readonly IHttpContextAccessor mHttpContextAccessor;

    public ChatPartyRepository(IHttpContextAccessor httpContextAccessor)
    {
        mHttpContextAccessor = httpContextAccessor;
    }
    
    public async Task<bool> HasOwnerAsync(ulong userId)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        return await dbContext.ChatParty.AnyAsync(p => p.OwnerUserId == userId);
    }
    
    public async Task<bool> ExistsAsync(string partyId)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        return await dbContext.ChatParty.AnyAsync(p => p.PartyId == partyId);
    }

    public async Task AddAsync(ChatParty chatParty)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        await dbContext.ChatParty.AddAsync(chatParty);
    }
    
    public async Task InviteAddAsync(string partyId, ulong userId)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        await dbContext.ChatPartyInvitation.AddAsync(new ChatPartyInvitation(partyId, userId));
    }

    public async Task<bool> MemberAddAsync(string partyId, ulong userId)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        
        var invitation = await dbContext.ChatPartyInvitation
            .FirstOrDefaultAsync(x => x.PartyId == partyId && x.UserId == userId);

        if (invitation is null)
            return false;
        
        dbContext.ChatPartyInvitation.Remove(invitation);
        await dbContext.ChatPartyMember.AddAsync(new ChatPartyMember(partyId, userId));
        
        return true;
    }
    
    public async Task<bool> DeleteAsync(string partyId, ulong userId)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();

        var party = await dbContext.ChatParty
            .FirstOrDefaultAsync(x => x.PartyId == partyId);

        if (party is null || party.OwnerUserId != userId)
            return false;

        var members = await dbContext.ChatPartyMember
            .Where(x => x.PartyId == partyId)
            .ToListAsync();

        var invitations = await dbContext.ChatPartyInvitation
            .Where(x => x.PartyId == partyId)
            .ToListAsync();

        dbContext.ChatPartyMember.RemoveRange(members);
        dbContext.ChatPartyInvitation.RemoveRange(invitations);
        dbContext.ChatParty.Remove(party);

        return true;
    }
}
