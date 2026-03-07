using Common.Concurrency;

namespace SwapQueue;

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
    
    static DoubleBufferedQueue<Packet> _mDoubleBufferedQueue = new();
    
    static async Task Main(string[] args)
    {
        _ = GeneratePacketAsync("A");      // Task A -> Enqueue
        _ = GeneratePacketAsync("B");      // Task B -> Enqueue   동시 접근 레이스 컨디션 조심
        _ = GeneratePacketAsync("C");
        _ = GeneratePacketAsync("D");
        _ = GeneratePacketAsync("E");

        while (true)
        {
            _mDoubleBufferedQueue.Process(HandlePacket);
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
            _mDoubleBufferedQueue.Enqueue(new Packet { PacketType = packetType, ThreadName = name });
            await Task.Delay(mGeneratePacketRandom.Next(500));
        }
    }
    
    private static void HandlePacket(Packet packet)
    {
        Console.WriteLine($"Packet{packet.ThreadName}: {packet.PacketType} 패킷 완료");
    }
}