using Common.Types;

namespace SwapQueue;

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

internal class Program
{
    enum EPacket
    {
        Move,
        Attack,
        Heal,
        Death,
        UseItem,
    }

    class Packet
    {
        public EPacket PacketType { get; set; }
        public string ThreadName { get; set; }
    }
    
    static SwapQueue<Packet> mSwapQueue = new();
    
    static async Task Main(string[] args)
    {
        _ = GeneratePacketAsync("A");      // Task A -> Enqueue
        _ = GeneratePacketAsync("B");      // Task B -> Enqueue   동시 접근 레이스 컨디션 조심
        _ = GeneratePacketAsync("C");
        _ = GeneratePacketAsync("D");
        _ = GeneratePacketAsync("E");

        while (true)
        {
            mSwapQueue.Process(HandlePacket);
        }
    }
    
    static async Task GeneratePacketAsync(string name)
    {
        var mPacketRandom = new Random();
        var mGeneratePacketRandom = new Random();
        var packetCnt = Enum.GetValues<EPacket>().Length;
        
        while (true)
        {
            var packetType = (EPacket)mPacketRandom.Next(packetCnt);
            mSwapQueue.Enqueue(new Packet { PacketType = packetType, ThreadName = name });
            await Task.Delay(mGeneratePacketRandom.Next(500));
        }
    }
    
    private static void HandlePacket(Packet packet)
    {
        Console.WriteLine($"Packet{packet.ThreadName}: {packet.PacketType} 패킷 완료");
    }
}