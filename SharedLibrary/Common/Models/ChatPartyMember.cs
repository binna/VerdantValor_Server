using System.ComponentModel.DataAnnotations;

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
}
