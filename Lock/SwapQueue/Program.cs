namespace SwapQueue;

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
}

class Program
{
    static Queue<Packet> WriteQueue;
    static Queue<Packet> ReadQueue;
    
    static Random packetRandom = new Random();
    static Random generatePacketRandom = new Random();
    
    static async Task Main(string[] args)
    {
        WriteQueue = new Queue<Packet>();
        ReadQueue = new Queue<Packet>();

        _ = GeneratePacketAsync();
        _ = GeneratePacketAsync();

        while (true)
        {
            Console.WriteLine($"읽기 큐에 {ReadQueue.Count}개 들어있음=====================");
            
            if (ReadQueue.Count > 0)
                ProcessPacket();
            else
                await Task.Delay(1000);

            Console.WriteLine("===========================================");
            
            (WriteQueue, ReadQueue) = (ReadQueue, WriteQueue);
        }
    }
    
    static async Task GeneratePacketAsync()
    {
        var packetCnt = Enum.GetValues<EPacket>().Length;
        
        while (true)
        {
            var packetType = (EPacket)packetRandom.Next(packetCnt);
            WriteQueue.Enqueue(new Packet { PacketType = packetType });
            
            await Task.Delay(generatePacketRandom.Next(500));
        }
    }

    static void ProcessPacket()
    {
        while (ReadQueue.Count > 0)
        {
            var packet = ReadQueue.Dequeue();
            Console.WriteLine($"{packet.PacketType} 패킷 처리 완료");
        }
    }
}