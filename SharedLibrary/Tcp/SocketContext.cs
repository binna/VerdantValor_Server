using System.Net.Sockets;
using VerdantValorShared.Packet;

namespace Tcp;

public class SocketContext
{
    private readonly TcpClient mClient;
    
    public Session Session { get; private set; }
    public Header Header { get; }
    public byte[] Payload { get; }
    
    public byte[] ReadBuffer { get; set; } = new byte[1024];
    public byte[] HeaderBuffer { get; set; } = new byte[Header.HEADER_SIZE];
    public byte[] PayloadBuffer { get; set; } = [];
        
    public int HeaderRead { get; set; } = 0;
    public int PayloadRead { get; set; } = 0;
    
    public int Offset { get; set; }
    public int Remaining { get; set; }
    
    public SocketContext(TcpClient tcpClient)
    {
        mClient = tcpClient;
    }

    public SocketContext(TcpClient tcpClient, Header header, byte[] payload)
    {
        mClient = tcpClient;
        Header = header;
        Payload = payload;
    }

    public void SessionChange(string sessionId, ulong userId) 
        => Session = new Session(sessionId, userId, mClient);
    
    public NetworkStream Stream => mClient.GetStream();
    public TcpClient tcpClient => mClient;
}