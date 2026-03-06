// Double Buffering 패턴

// [문제] Queue<T>는 기본적으로 thread-safe가 아니다.
//      여러 스레드가 동시에 Enqueue와 Dequeue 하면 
//      처리 순서를 보장할 수 없고, 레이스 컨디션이 발생할 수 있다.

// 왜 위험한가?
//      큐에 데이터를 넣는 것은 하나의 원자적 연산이 아니라,
//      만들기 -> 자리 확인 -> 넣기와 같은 여러 단계로 이루어진다.
//      이 과정에서 여러 스레드가 동시에 접근하면 레이스 컨디션이 발생할 수 있다.

// 결론
//      공유 자원을 사용하는 순간, 항상 레이스 컨디션 가능성을 먼저 가정해야 한다.

namespace Common.Concurrency;

public class DoubleBufferedQueue<T>
{
    private Queue<T> mInputQueue = new();
    private Queue<T> mOutputQueue = new();
    
    private readonly object mLock = new();
    
    // 0: false
    // 1: true
    private int mProcessing;

    // 생산자 (여러 스레드에서 패킷을 넣을 수 있음)
    public void Enqueue(T packet)
    {
        lock (mLock)
        {
            mInputQueue.Enqueue(packet);
        }
    }

    // 소비자 (보통 한 스레드)
    // 한번에 모아서 처리
    public void Process(Action<T> handlePacket)
    {
        if (handlePacket == null)
            throw new ArgumentNullException(nameof(handlePacket));
        
        if (Interlocked.Exchange(ref mProcessing, 1) == 1)
            return;

        try
        {
            lock (mLock)
            {
                if (mInputQueue.Count == 0)
                    return;

                (mInputQueue, mOutputQueue) = (mOutputQueue, mInputQueue);
            }

            // 락 없이 outputQueue 전부 처리
            Console.WriteLine($"총 {mOutputQueue.Count}개 =====================");
            var cnt = 1;
            while (mOutputQueue.Count > 0)
            {
                var packet = mOutputQueue.Dequeue();
                Console.Write($"{cnt}-");
                handlePacket(packet);
                cnt++;
            }
            Console.WriteLine($"===============================================");
        }
        finally
        {
            Interlocked.Exchange(ref mProcessing, 0);
        }
    }
}