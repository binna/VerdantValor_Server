using System.Net.Sockets;

namespace SharedLibrary.Tcp;

public class Session
{
    private readonly TcpClient mClient;
    
    public string SessionId { get; }
    public ulong UserId { get; }
    public int RoomId { get; set; }

    public Session(string sessionId, ulong userId, TcpClient client)
    {
        SessionId = sessionId;
        UserId = userId;
        mClient = client;
    }

    public NetworkStream Stream => mClient.GetStream();
    public void Disconnect() => mClient.Dispose();
}