namespace SwapQueue;

internal enum EPacket
{
    Move,
    Attack,
    Heal,
    Death,
    UseItem,
}

internal class Packet
{
    public EPacket PacketType { get; set; }
}

internal class Program
{
    static Queue<Packet> mWriteQueue;
    static Queue<Packet> mReadQueue;
    
    static async Task Main(string[] args)
    {
        mWriteQueue = new Queue<Packet>();
        mReadQueue = new Queue<Packet>();

        _ = GeneratePacketAsync();
        _ = GeneratePacketAsync();

        while (true)
        {
            Console.WriteLine($"읽기 큐에 {mReadQueue.Count}개 들어있음 ====================");
            
            if (mReadQueue.Count > 0)
                ProcessPacket();
            else
                await Task.Delay(1000);

            Console.WriteLine("===========================================");
            
            (mWriteQueue, mReadQueue) = (mReadQueue, mWriteQueue);
        }
    }
    
    static async Task GeneratePacketAsync()
    {
        var mPacketRandom = new Random();
        var mGeneratePacketRandom = new Random();
        var packetCnt = Enum.GetValues<EPacket>().Length;
        
        while (true)
        {
            var packetType = (EPacket)mPacketRandom.Next(packetCnt);
            mWriteQueue.Enqueue(new Packet { PacketType = packetType });
            
            await Task.Delay(mGeneratePacketRandom.Next(500));
        }
    }

    static void ProcessPacket()
    {
        while (mReadQueue.Count > 0)
        {
            var packet = mReadQueue.Dequeue();
            Console.WriteLine($"{packet.PacketType} 패킷 처리 완료");
        }
    }
}