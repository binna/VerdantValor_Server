using System.ComponentModel.DataAnnotations;

namespace Common.Models;

public class ChatPartyInvitation
{
    [MaxLength(32)]
    public string PartyId { get; private set; }

    public ulong UserId { get; private set; }


    public ChatPartyInvitation() { }

    public ChatPartyInvitation(string partyId, ulong userId)
    {
        PartyId = partyId;
        UserId = userId;
    }
}
