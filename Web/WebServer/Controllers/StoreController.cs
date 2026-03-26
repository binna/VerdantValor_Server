using Common.Manager;
using Common.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Protocol.Web.Dtos;
using Shared.Constants;
using Shared.Types;
using WebServer.Services;

namespace WebServer.Controllers;

[Route($"{AppConstant.WEB_SERVER_API_BASE}/[controller]")]
[ApiController]
public class StoreController : Controller
{
    private readonly ILogger<StoreController> mLogger;
    private readonly StoreService mStoreService;
    
    public StoreController(
        ILogger<StoreController> logger,
        StoreService storeService)
    {
        mLogger = logger;
        mStoreService = storeService; 
    }

    [HttpPost("Buy")]
    [Authorize(Policy = "SessionPolicy")]
    public async Task<ApiResponse> Buy([FromBody] BuyReq request)
    {
        if (!GameDataManager.StoreTable.TryGet(request.StoreId, out var store))
            return ApiResponse.From(EResponseResult.StoreNotFound);
        
        if (!ulong.TryParse(this.GetUserId(), out var userId))
            return ApiResponse.From(EResponseResult.InvalidUserId);
       
        return await mStoreService.BuyAsync(store, userId);
    }
}