namespace Common.Types;

public class SwapQueue<T>
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