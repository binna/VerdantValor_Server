using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace Common.Models;

public class ChatPartyMember
{
    [MaxLength(32)]
    public string PartyId { get; private set; }

    public ulong UserId { get; private set; }


    public ChatPartyMember() { }

    public ChatPartyMember(string partyId, ulong userId)
    {
        PartyId = partyId;
        UserId = userId;
    }
    
    public static async Task<string> FromDbDataReaderToPartyIdAsync(DbDataReader reader, CancellationToken token = default)
    {
        if (!await reader.ReadAsync(token))
            return null;
        
        var partyIdIdx = reader.GetOrdinal("partyId");
        
        return await reader.GetFieldValueAsync<string>(partyIdIdx, token);
    }
}
