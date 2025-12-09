using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatServer.Client;

internal class Client
{
    static void Main(string[] args)
    {
        var socket = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var endPoint = new IPEndPoint(IPAddress.Loopback, 20000);

        var arg = new SocketAsyncEventArgs();
        arg.RemoteEndPoint = endPoint;
        arg.Completed += ConnectCompleted;

        var bConnectPending = socket.ConnectAsync(arg);
        if (!bConnectPending)
            ConnectCompleted(socket, arg);

        // 프로그램 종료 방지용 루프
        while (true)
            Thread.Sleep(1000);
    }
    
    private static void ConnectCompleted(object? sender, SocketAsyncEventArgs e)
    {
        var socket = (Socket)sender!;
        
        // Connect용 arg는 이제 필요 없음
        e.Dispose();

        // Send 전용 arg 준비
        var arg = new SocketAsyncEventArgs();
        arg.Completed += SendCompleted;

        // 전송 이벤트 등록
        SendCompleted(socket, arg);
    }

    private static void SendCompleted(object? sender, SocketAsyncEventArgs e)
    {
        var socket = (Socket)sender!;
    
        // 콘솔에서 보낼 문자열 입력 대기
        var str = Console.ReadLine();
        
        // 콘솔에서 받은 문자열이 null일 때 
        //  콘솔이 닫혔거나 입력 스트림이 종료된 상태
        if (str is null)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Dispose();
            e.Dispose();
            return;
        }
        
        // 버퍼 세팅
        var buffer = Encoding.UTF8.GetBytes(str);
        e.SetBuffer(buffer, 0, buffer.Length);
            
        // 비동기 Send 등록
        var bSendPending = socket.SendAsync(e);
        
        // I/O가 즉시 끝난 경우에는 Completed 이벤트가 안 불리므로 직접 재호출
        if (!bSendPending)
            SendCompleted(socket, e);
    }
}