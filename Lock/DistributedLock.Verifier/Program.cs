using RedisLock = Redis.Implementations;

namespace DistributedLock.Verifier;

class Program
{
    const int REPEAT_NUM = 5;
    
    static async Task Main(string[] args)
    {
        //await TestDistributedLockStackExchange();
        
        {
            List<Task> tasks = [];
            var distributedLock = new RedisLock.DistributedLockRawRedis(
                "localhost", 6379, 10, 3000);
            
            for (var i = 0; i < REPEAT_NUM; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var cts = new CancellationTokenSource();
                    
                    var bSuccess = await distributedLock
                        .TryAcquireLockAsync("GainItem", "shine", cts.Token);
            
                    Console.WriteLine(
                        $"{(bSuccess ? "Success" : "Fail")} Acquire Lock//gain item//shine");
                }));
            }
            
            for (var i = 0; i < REPEAT_NUM; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var cts = new CancellationTokenSource();
                    
                    var bSuccess = await distributedLock
                        .TryReleaseLockAsync("GainItem", "shine", cts.Token);
            
                    Console.WriteLine(
                        $"{(bSuccess ? "Success" : "Fail")} --> Release Lock//gain item//shine");
                }));
            }
        
            await Task.WhenAll(tasks);
        }
        
        Console.WriteLine("===============================================================================");
        Console.WriteLine();
        Console.WriteLine("TTL 테스트 ====================================================================");

        {
            var distributedLock = new RedisLock.DistributedLockRawRedis(
                "localhost", 6379, 10, 1);
            
            using var cts = new CancellationTokenSource();
            
            var bSuccess = await distributedLock
                .TryAcquireLockAsync("GainItem", "shine", cts.Token);

            Console.WriteLine(
                $"{(bSuccess ? "Success" : "Fail")} Acquire Lock//gain item//shine");

            await Task.Delay(2);
            
            bSuccess = await distributedLock
                .TryReleaseLockAsync("GainItem", "shine", cts.Token);

            Console.WriteLine(
                $"{(bSuccess ? "Success" : "Fail")} --> Release Lock//gain item//shine");
        }

        Console.WriteLine("===============================================================================");
    }

    public static async Task TestDistributedLockStackExchange()
    {
        Console.WriteLine("기능 테스트 ===================================================================");

        {
            List<Task> tasks = [];
            var distributedLock = new RedisLock.DistributedLockStackExchange(
                "localhost", "6379", 10, 3000);
        
            for (var i = 0; i < REPEAT_NUM; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var bSuccess = await distributedLock
                        .TryAcquireLockAsync("GainItem", "shine");

                    Console.WriteLine(
                        $"{(bSuccess ? "Success" : "Fail")} Acquire Lock//gain item//shine");
                }));
            }
        
            for (var i = 0; i < REPEAT_NUM; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var bSuccess = await distributedLock
                        .TryReleaseLockAsync("GainItem", "shine");

                    Console.WriteLine(
                        $"{(bSuccess ? "Success" : "Fail")} --> Release Lock//gain item//shine");
                }));
            }
        
            await Task.WhenAll(tasks);
        }

        Console.WriteLine("===============================================================================");
        Console.WriteLine();
        Console.WriteLine("TTL 테스트 ====================================================================");

        {
            var distributedLock = new RedisLock.DistributedLockStackExchange(
                "localhost", "6379", 10, 1);
            
            var bSuccess = await distributedLock
                .TryAcquireLockAsync("GainItem", "shine");

            Console.WriteLine(
                $"{(bSuccess ? "Success" : "Fail")} Acquire Lock//gain item//shine");

            await Task.Delay(2);
            
            bSuccess = await distributedLock
                .TryReleaseLockAsync("GainItem", "shine");

            Console.WriteLine(
                $"{(bSuccess ? "Success" : "Fail")} --> Release Lock//gain item//shine");
        }

        Console.WriteLine("===============================================================================");
    }
}