using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatServer.Client;

public class SocketClient
{
    private readonly int mPort;
    
    private TcpClient? mTcpClient;
    private CancellationTokenSource mCts = new();
    
    public SocketClient(int port)
    {
        mPort = port;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (mTcpClient != null)
        {
            throw new InvalidOperationException("Client is already running.");
        }
        
        mCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var cancelToken = mCts.Token;
        
        mTcpClient = new TcpClient(IPAddress.Loopback.ToString(), mPort);;

        var readTask  = HandleClientSendAsync(mTcpClient, cancelToken);
        var writeTask = Task.Run(() => HandleClientWriteAsync(mTcpClient, cancelToken), cancelToken);

        await Task.WhenAny(readTask, writeTask);
    }
    
    private void HandleClientWriteAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var stream = tcpClient.GetStream();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var str = Console.ReadLine();

            if (str == null)
                return;

            var buffer = Encoding.UTF8.GetBytes(str);
            stream.WriteAsync(buffer, cancellationToken);
        }
    }
    
    private async Task HandleClientSendAsync(TcpClient tcpClient, CancellationToken cancellationToken)
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
            Console.WriteLine($"받음 : {msg}");
        }
    }
    
    public void StopAsync()
    {
        Console.WriteLine("호출!");
        if (mTcpClient == null)
            return;
        
        mCts.Cancel();
        
        mTcpClient.Dispose();
        mTcpClient = null;
    }
}