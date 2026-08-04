using Common.Models;

namespace Efcore.Repositories;

public interface IChatPartyRepository
{
    Task<bool> ExistsAsync(string partyId);
    Task<bool> IsOwnerAsync(ulong ownerUserId);
    Task<bool> IsMemberAsync(ulong userId);
    Task<string?> FindPartyIdByOwnerUserIdAsync(ulong ownerUserId);
    Task AddAsync(ChatParty chatParty);
    Task InviteAddAsync(string partyId, ulong invitedUserId);
    Task<bool> MemberAddAsync(string partyId, ulong invitedUserId);
    Task<bool> DeleteAsync(ulong ownerUserId);
}
