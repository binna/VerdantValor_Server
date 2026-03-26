using Common.Types;
using Shared.Types;

namespace Common.Models;

public class Inventory
{
    public ulong Id { get; private set; }
    
    public int ItemId { get; private set; }
    
    public EInventoryState State { get; private set; }
    
    public ulong UserId { get; private set; }   // FK
    
    public ServerDateTime CreatedAt { get; private set; }
    
    public ServerDateTime UpdatedAt { get; private set; }
    
    public ServerDateTime? ExpiredAt { get; private set; }
    
    public ServerDateTime? UsedAt { get; private set; }

    
    public Inventory() { }

    public Inventory(int itemId, ulong userId, ServerDateTime? expiredAt = null)
    {
        // TODO 아이템, 이거 기간제가 있는지
        var now = ServerDateTime.Now;
        ItemId = itemId;
        State = EInventoryState.Available;
        UserId = userId;
        CreatedAt = now;
        UpdatedAt = now;
        ExpiredAt = expiredAt;
    }

    public void UseItem()
    {
        State = EInventoryState.Used;
        UsedAt = ServerDateTime.Now;
    }
    
    public void CheckExpired()
    {
        if (ExpiredAt >= ServerDateTime.Now)
            State = EInventoryState.Expired;
    }
}