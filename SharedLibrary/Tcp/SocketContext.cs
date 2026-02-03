using System.Net.Sockets;
using Protocol.Chat.Frames;
using Shared.Constants;

namespace Tcp;

public class SocketContext
{
    private readonly TcpClient mClient;

    public bool IsLogin { get; set; }
    
    public Session Session { get; private set; }
    public PacketHeader Header { get; set; }
    
    public byte[] ReadBuffer { get; } = new byte[1024];
    public byte[] HeaderBuffer { get; } = new byte[AppConstant.HEADER_SIZE];
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