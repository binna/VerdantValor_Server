using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;

namespace Common.Models;

public class ChatParty
{
    public enum EState
    {
        Available,
        Deleted,
    }

    [MaxLength(32)]
    public string PartyId { get; private set; }
    
    [MaxLength(50)]
    public string Name { get; private set; } = string.Empty;
    
    public ulong OwnerUserId { get; private set; }

    [NotMapped] 
    public EState State { get; set; } = EState.Available;

    
    private ChatParty() { }
    
    public ChatParty(string name, ulong ownerUserId)
    {
        PartyId = Guid.NewGuid().ToString("N");
        Name = name;
        OwnerUserId = ownerUserId;
    }

    public static async Task<ChatParty> FromDbDataReaderAsync(DbDataReader reader, CancellationToken token = default)
    {
        if (!await reader.ReadAsync(token))
            return null;
        
        var partyIdIdx = reader.GetOrdinal("partyId");
        var nameIdx = reader.GetOrdinal("name");
        var ownerUserIdIdx = reader.GetOrdinal("ownerUserId");

        return new ChatParty
        {
            PartyId =
                await reader.GetFieldValueAsync<string>(partyIdIdx, token),
            Name =
                await reader.GetFieldValueAsync<string>(nameIdx, token),
            OwnerUserId = 
                await reader.GetFieldValueAsync<ulong>(ownerUserIdIdx, token),
        };
    }
    
    public static async Task<List<string>> FromDbDataReaderToPartyIdListAsync(DbDataReader reader, CancellationToken token = default)
    {
        List<string> parties = [];
        
        var partyIdIdx = reader.GetOrdinal("partyId");

        while (await reader.ReadAsync(token))
        {
            parties.Add(await reader.GetFieldValueAsync<string>(partyIdIdx, token));
        }

        return parties;
    }
    
    public static async Task<string> FromDbDataReaderToPartyIdAsync(DbDataReader reader, CancellationToken token = default)
    {
        if (!await reader.ReadAsync(token))
            return null;
        
        var partyIdIdx = reader.GetOrdinal("partyId");
        
        return await reader.GetFieldValueAsync<string>(partyIdIdx, token);
    }
}
