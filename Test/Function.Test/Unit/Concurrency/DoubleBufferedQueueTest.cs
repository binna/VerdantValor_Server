using Common.Concurrency;

namespace Function.Test.Unit.Concurrency;

public class DoubleBufferedQueueTest
{
    private DoubleBufferedQueue<int> CreateSut()
    {
        return new DoubleBufferedQueue<int>();
    }
    
    // null 핸들러 -> 예외 발생
    [Fact]
    public void Test_Process_HandlePacket이_null이면_ArgumentNullException_Throw()
    {
        var sut = CreateSut();
        
        Assert.Throws<ArgumentNullException>(() => sut.Process(null));
    }
    
    // 빈 큐는 처리 안됨
    [Fact]
    public void Test_Process_큐가_비어있으면_handlePackt이_호출되지_않음()
    {
        var sut = CreateSut();
        int callCount = 0;

        sut.Process(_ => callCount++);
        
        Assert.Equal(0, callCount);
    }
    
    // Enqueue -> Process -> 처리
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void Test_Enqueue_후_Process_호출시_모든_아이템이_처리됨(int count)
    {
        var sut = CreateSut();
        var proecessed = new List<int>();

        for (int i = 0; i < count; i++)
        {
            sut.Enqueue(i);
        }
        
        sut.Process(i => proecessed.Add(i));

        Assert.Equal(count, proecessed.Count);
    }
    
    // 순서 보장
    [Fact]
    public void Test_Enqueue한_순서대로_처리됨()
    {
        var sut = CreateSut();
        var proecessed = new List<int>();
        
        sut.Enqueue(1);
        sut.Enqueue(2);
        sut.Enqueue(3);
        
        sut.Process(i => proecessed.Add(i));
        
        Assert.Equal(new []{1, 2, 3}, proecessed);   
    }
    
    // Double Buffering 동작 검증 테스트
    // Process 실행중 Enqueue한 항목은 이번 사이클에 포함되지 않아야 한다.

    [Fact]
    public void Test_Process_처리_도중_Enqueue한_항목은_이번_사이클에_포함_되지_않음()
    {
        var sut = CreateSut();
        var proecessed = new List<int>();
        
        sut.Enqueue(1);
        sut.Enqueue(2);
        sut.Process(i =>
        {
            proecessed.Add(i);
            
            // 처리 도중 새 항목 추가
            sut.Enqueue(100);
        });

        // 이번 Process에서는 1, 2만 처리됨
        Assert.Equal(new []{1, 2}, proecessed);  
    }
    
    // Process가 끝난 후 다음 Process 호출에서 앞서 쌓인 항목이 처리되어야 한다.
    [Fact]
    public void Test_Process_처리_도중_Enqueue한_항목은_다음_Process에서_처리됨()
    {
        var sut = CreateSut();
        var proecessed = new List<int>();
        
        sut.Enqueue(1);
        sut.Process(i =>
        {
            proecessed.Add(i);
            
            // 처리 도중 새 항목 추가
            sut.Enqueue(100);
        });

        sut.Process(i => proecessed.Add(i));
        
        Assert.Equal(new []{1, 100}, proecessed); 
    }

    [Fact]
    public async Task Test_Process_실행_중_동시_Process_호출은_무시됨()
    {
        var sut = CreateSut();
        var processedCount = 0;
        
        sut.Enqueue(1);
        sut.Enqueue(2);
        sut.Enqueue(3);
        
        // 첫번째 프로세스 처리중
        var firstProcess = Task.Run(() =>
        {
            sut.Process(i =>
            {
                Interlocked.Increment(ref processedCount);
                Task.Delay(100); // 처리 시간 시뮬레이션
            });
        });

        await Task.Delay(10);
        sut.Process(_ => Interlocked.Increment(ref processedCount));
        
        await firstProcess;
        
        // 두번째 Process는 무시되었으므로 총 3번만 처리됨
        Assert.Equal(3, processedCount);
    }
    
    // N개의 스레드가 동시에 Enqueue해도 데이터 유실 없이 전부 처리 되어야 한다
    [Theory]
    [InlineData(10, 100)]
    [InlineData(50, 200)]
    public async Task Test_Enqueue_멀티스레드_동시_Enqueue시_데이터_유실_없음(int threadCount, int itemsPerThread)
    {
        var sut = CreateSut();
        var totalExpected = threadCount * itemsPerThread;
        
        // N개 스레드가 동시에 Enqueue
        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < itemsPerThread; i++)
                {
                    sut.Enqueue(i);
                }
            })).ToList();
        
        await Task.WhenAll(tasks);
        
        // 전부 처리됐는지?
        var processedCount = 0;
        sut.Process(_ => Interlocked.Increment(ref processedCount));
        Assert.Equal(totalExpected, processedCount);
    }
}