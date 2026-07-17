using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace Common.Models;

public class ChatParty
{
    [MaxLength(32)]
    public string PartyId { get; private set; }
    
    [MaxLength(50)]
    public string Name { get; private set; } = string.Empty;

    
    private ChatParty() { }
    
    public ChatParty(string name)
    {
        Name = name;
    }

    public static async Task<ChatParty> FromDbDataReaderAsync(DbDataReader reader, CancellationToken token = default)
    {
        if (!await reader.ReadAsync(token))
            return null;
        
        var partyIdIdx = reader.GetOrdinal("partyId");
        var nameIdx = reader.GetOrdinal("name");

        return new ChatParty
        {
            PartyId =
                await reader.GetFieldValueAsync<string>(partyIdIdx, token),
            Name =
                await reader.GetFieldValueAsync<string>(nameIdx, token),
        };
    }
    
    public static async Task<List<string>> FromDbDataReaderToPartyIdListAsync(DbDataReader reader, CancellationToken token = default)
    {
        if (!await reader.ReadAsync(token))
            return null;

        List<string> parties = [];
        
        var partyIdIdx = reader.GetOrdinal("partyId");

        while (await reader.ReadAsync(token))
        {
            parties.Add(await reader.GetFieldValueAsync<string>(partyIdIdx, token));
        }

        return parties;
    }
}