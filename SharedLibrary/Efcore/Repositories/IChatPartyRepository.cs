using Common.Models;

namespace Efcore.Repositories;

public interface IChatPartyRepository
{
    Task<bool> HasOwnerAsync(ulong ownerUserId);
    Task<bool> ExistsAsync(string partyId);
    Task<bool> IsOwnerAsync(string partyId, ulong ownerUserId);
    Task AddAsync(ChatParty chatParty);
    Task InviteAddAsync(string partyId, ulong invitedUserId);
    Task<bool> MemberAddAsync(string partyId, ulong invitedUserId);
    Task<bool> DeleteAsync(ulong ownerUserId);
}
