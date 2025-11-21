using System.Net.Sockets;

namespace SharedLibrary.Tcp.State;

public class ReceiveState
{
    public Socket Client { get; }
    public byte[] Buffer { get; }

    public ReceiveState(Socket client, int bufferSize = 4096)
    {
        Client = client;
        Buffer = new byte[bufferSize];
    }
}