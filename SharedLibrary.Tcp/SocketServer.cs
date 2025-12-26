using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SharedLibrary.Tcp;

public class SocketServer
{
    private readonly IPAddress mIp = IPAddress.Any;
    private readonly int mPort;
    
    private TcpListener? mListener;
    private CancellationTokenSource mCts = new();
    
    #region TODO 별도의 ClientSessionManager 만들어야 함
    private static ConcurrentDictionary<ulong, TcpClient> mConnectedUsers = [];
    private static ConcurrentDictionary<string, List<TcpListener>> mRoomList = [];
    #endregion

    private static ulong id = 0;
    
    public SocketServer(int port)
    {
        mPort = port;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (mListener != null)
            throw new InvalidOperationException("Server is already running.");
        
        mCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var cancelToken = mCts.Token;
        
        mListener = new TcpListener(mIp, mPort);
        mListener.Start();

        while (!cancelToken.IsCancellationRequested)
        {
            var tcpClient = await mListener.AcceptTcpClientAsync(cancelToken);
            Console.WriteLine($"[SERVER] Client connected: {tcpClient.Client.RemoteEndPoint}");
            mConnectedUsers.TryAdd(id++, tcpClient);
            Console.WriteLine(">> " + mConnectedUsers.Count);
            _ = HandleClientAsync(tcpClient, cancelToken);
        }
    }
    
    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var stream = tcpClient.GetStream();
        var buffer = new byte[1024];

        while (!cancellationToken.IsCancellationRequested)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);

            if (read == 0)
            {
                Console.WriteLine("[SERVER] Client disconnected");
                break;
            }
            
            var msg = Encoding.UTF8.GetString(buffer, 0, read);
            Console.WriteLine($"{msg}");

            foreach (var client in mConnectedUsers)
            {
                Console.WriteLine("발송된다!");
                var a = client.Value.GetStream();
                await a.WriteAsync(Encoding.UTF8.GetBytes(msg), cancellationToken);
            }
        }
    }
    
    public void StopAsync()
    {
        if (mListener == null)
            return;
        
        mCts.Cancel();
        
        mListener.Stop();
        mListener = null;

        foreach (var client in mConnectedUsers)
        {
            if (mConnectedUsers.TryRemove(client.Key, out _))
                client.Value.Dispose();
        }

        mRoomList.Clear();
    }
}