using Common.Models;

namespace Ado.Daos;

public interface IChatPartyDao
{
    Task<List<string>> FindAllPartyIdAsync(CancellationToken token);
    Task<string?> FindPartyIdByOwnerUserIdAsync(ulong ownerUserId, CancellationToken token);
    Task<string?> FindPartyIdByMemberUserIdAsync(ulong userId, CancellationToken token);
    Task<ChatParty?> FindByPartyIdAsync(string partyId, CancellationToken token);
}