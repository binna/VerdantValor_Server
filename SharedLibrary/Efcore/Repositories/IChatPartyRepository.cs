using Common.Models;

namespace Efcore.Repositories;

public interface IChatPartyRepository
{
    Task<bool> HasOwnerAsync(ulong userId);
    Task<bool> ExistsAsync(string partyId);
    Task AddAsync(ChatParty chatParty);
    Task InviteAddAsync(string partyId, ulong userId);
    Task<bool> MemberAddAsync(string partyId, ulong userId);
    Task<bool> DeleteAsync(string partyId, ulong userId);
}
