using Common.Types;
using Shared.Types;

namespace Common.Models;

public class Purchase
{
    public ulong PurchaseId { get; private set; }
    
    public int StoreId { get; private set; }
    
    public EPurchaseState PurchaseState { get; private set; }
    
    public ulong UserId { get; private set; }   // FK
    
    public ServerDateTime CreatedAt { get; private set; }
    
    public ServerDateTime UpdatedAt { get; private set; }
    
    public ServerDateTime? CompletedAt { get; private set; }
    
    
    private Purchase() { }

    public Purchase(int storeId, ulong userId)
    { 
        var now = ServerDateTime.Now;
        StoreId = storeId;
        PurchaseState = EPurchaseState.InProgress;
        UserId = userId;
        CreatedAt = now;
        UpdatedAt = now;
    }
}