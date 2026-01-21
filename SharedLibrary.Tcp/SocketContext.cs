using System.Net.Sockets;
using SharedLibrary.Protocol.Packet;

namespace SharedLibrary.Tcp;

public class SocketContext
{
    public TcpClient TcpClient { get; }
    public Session Session { get; private set; }
    public Header Header { get; }
    public byte[] Payload { get; }
    
    public SocketContext(TcpClient tcpClient, Session session, Header header, byte[] payload)
    {
        TcpClient = tcpClient;
        Session = session;
        Header = header;
        Payload = payload;
    }

    public void SessionChange(string sessionId, ulong userId, TcpClient tcpClient) 
        => Session = new Session(sessionId, userId, tcpClient);
}