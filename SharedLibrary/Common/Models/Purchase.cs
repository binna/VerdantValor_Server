using Common.Types;
using Shared.Types;

namespace Common.Models;

public class Purchase
{
    public int PurchaseId { get; private set; }
    
    public int StoreId { get; private set; }
    
    public EPurchaseState PurchaseState { get; private set; }
    
    public ulong UserId { get; private set; }   // FK
    
    public ServerDateTime CreatedAt { get; private set; }
    
    public ServerDateTime UpdatedAt { get; private set; }
    
    public ServerDateTime? CompletedAt { get; private set; }
}