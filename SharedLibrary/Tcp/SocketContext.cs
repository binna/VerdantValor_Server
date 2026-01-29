using System.Net.Sockets;
using VerdantValorShared.Packet;

namespace Tcp;

public class SocketContext
{
    private readonly TcpClient mClient;

    public bool IsLogin { get; set; } = false;
    
    public Session Session { get; private set; }
    public Header Header { get; } = new();
    
    public byte[] ReadBuffer { get; } = new byte[1024];
    public byte[] HeaderBuffer { get; } = new byte[Header.HEADER_SIZE];
    public byte[] PayloadBuffer { get; set; } = [];
        
    public int HeaderRead { get; set; }
    public int PayloadRead { get; set; }
    
    public int Offset { get; set; }
    public int Remaining { get; set; }
    
    public SocketContext(TcpClient client)
    {
        mClient = client;
    }

    public void SetSession(string sessionId, ulong userId) 
        => Session = new Session(sessionId, userId, mClient);
    
    public NetworkStream Stream => mClient.GetStream();
    public TcpClient Client => mClient;
}