using System.Net.Sockets;

namespace Tcp;

public class Session
{
    private readonly TcpClient mClient;
    
    public string SessionId { get; }
    public ulong UserId { get; }

    public string? CurrentWorld { get; set; } = null;
    public string? CurrentParty { get; set; } = null;

    public Session(string sessionId, ulong userId, TcpClient client)
    {
        SessionId = sessionId;
        UserId = userId;
        mClient = client;
    }

    public NetworkStream Stream => mClient.GetStream();
    public void Disconnect() => mClient.Close();
    public bool IsAlive() => NetworkSocket.IsSocketAlive(mClient);
}