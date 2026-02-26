using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Redis.Implementations;
using Shared.Constants;

namespace WebServer.Controllers;

[Route($"{AppConstant.WEB_SERVER_API_BASE}/[controller]")]
[ApiController]
public class StoreController : Controller
{
    private readonly DistributedLockStackExchange mRedis;
    
    public StoreController()
    {
        mRedis = new("localhost", "6379", 10, 1000);
    }

    [HttpGet("Buy")]
    [AllowAnonymous]
    public async void Buy()
    {
        // TODO 생애 최소 구매를 여러번 호출 시 어떻게 동기화를 보장 할 것인가?
        //  간단하게 레디스 분산락을 이용한다
        //  그렇지만 ttl이 만료됬을때를 고려해야 한다
        //  1. 분산락 획득한다
        //  2. DB에 먼저 데이터를 만든다 (이때 결제중으로 만들어 둔다) 
        //  3. 구매를 완료하고 상태값을 구매 완료로 바꾼다.
        //  4. 분산락 해제한다
        //  만약 중간에 TTL이 만료되거나 문제가 있다면 구매 불가하게 만들 예정
        //  (관리자를 통해서만 가능하도록)
    }
}