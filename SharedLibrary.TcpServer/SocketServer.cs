using System.Net;
using System.Net.Sockets;
using System.Text;
using SharedLibrary.Tcp.State;

namespace SharedLibrary.Tcp;

public class SocketServer
{
    private static Socket mSocket;
    private static List<Socket> mConnectedClient = new();

    public static void StartServer(int port, int maxConnections)
    {
        mSocket = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        var ipEndPoint = new IPEndPoint(IPAddress.Any, port);
        
        mSocket.Bind(ipEndPoint);
        mSocket.Listen(maxConnections);
        mSocket.BeginAccept(AcceptCallback, null);
    }

    private static void AcceptCallback(IAsyncResult asyncResult)
    {
        // TODO 커넥션 확인하고, 확인한 커넥션을 나중에 저장하는 로직이 필요할 것 같아 보임
        var client = mSocket.EndAccept(asyncResult);
        /////////////////////////////////////////////////////////////////////////
        
        var receiveState = new ReceiveState(client);
        
        client.BeginReceive(
            receiveState.Buffer,
            0,
            receiveState.Buffer.Length,
            SocketFlags.None,
            ReceiveCallback,
            receiveState
        );
        
        mSocket.BeginAccept(AcceptCallback, null);
    }
    
    private static void ReceiveCallback(IAsyncResult asyncResult)
    {
        var state  = (ReceiveState)asyncResult.AsyncState;
        var client = state.Client;

        int received;

        try
        {
            received = client.EndReceive(asyncResult);
        }
        catch (SocketException)
        {
            client.Close();
            mConnectedClient.Remove(client);
            return;
        }

        if (received <= 0)
        {
            client.Close();
            mConnectedClient.Remove(client);
            return;
        }

        var json = Encoding.UTF8.GetString(state.Buffer, 0, received);
        Console.WriteLine($"[SERVER] Received: {json}");

        client.BeginReceive(
            state.Buffer,
            0,
            state.Buffer.Length,
            SocketFlags.None,
            ReceiveCallback,
            state
        );
    }
}