using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatServer;

internal class Server
{
    static void Main(string[] args)
    {
        var serverSocket = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var endPoint = new IPEndPoint(IPAddress.Loopback, 20000);

        serverSocket.Bind(endPoint);
        serverSocket.Listen(10);

        // SocketAsyncEventArgs
        //  비동기 소켓 작업에 필요한 정보를 담는 컨텍스트 객체
        //  ㄴ (비동기 작업 1개에 필요한 모든 상태와 결과를 담아두는 객체)
        var arg = new SocketAsyncEventArgs();
        
        // 비동기 작업이 끝났을 때 호출할 이벤트 핸들러를 등록
        arg.Completed += AcceptCompleted;
        
        // 비동기로 클라이언트 접속 기다림
        // [오해 금지] 이벤트 기반 비동기(IOCP 기반) 패턴 사용
        var pending = serverSocket.AcceptAsync(arg);
        // AcceptAsync는
        //  논리적으로는 연결 수락이지만
        //  OS 관점에서는 네트워크 소켓에 대한 I/O 작업으로 처리된다

        // I/O가 즉시 완료되면 false를 반환하고 Completed 이벤트가 호출되지 않음
        // 그 경우를 위해 직접 AcceptCompleted를 호출해 동일한 흐름으로 처리
        if (!pending)
            AcceptCompleted(serverSocket, arg);

        // 프로그램 종료 방지용 루프
        while (true)
            Thread.Sleep(1000);
        
        // Main 스레드
        //  - AcceptAsync를 호출해 I/O 작업을 OS(IOCP)에 등록

        // IOCP/ThreadPool 스레드
        //  - 클라이언트 접속 발생
        //  - Accept I/O 완료 시 AcceptCompleted(sender, args) 호출
    }
    
    private static void AcceptCompleted(object? sender, SocketAsyncEventArgs e)
    {
        // 연결된 클라 정보 확인
        var serverSocket = (Socket)sender!;
        var clientSocket = e.AcceptSocket;

        if (clientSocket != null)
        {
            Console.WriteLine($"{clientSocket.RemoteEndPoint}");
            
            // 방금 들어온 클라이언트에 대해 Receive 시작
            var arg = new SocketAsyncEventArgs();
            arg.Completed += ReceiveCompleted;
            var buffer = new byte[256];
            arg.SetBuffer(buffer, 0, buffer.Length);
    
            var bReceivePending = clientSocket.ReceiveAsync(arg);
            if (!bReceivePending)
                ReceiveCompleted(clientSocket, arg);
        }

        // 다음 클라이언트를 받기 위한 재등록
        // pending: 해당 I/O 작업이 OS에서 아직 진행 중인지 여부
        e.AcceptSocket = null;
        var bAcceptPending = serverSocket.AcceptAsync(e);
        
        // Accept가 즉시 완료된 경우
        // Completed 이벤트가 호출되지 않으므로 직접 콜백 호출
        if (!bAcceptPending)
            AcceptCompleted(serverSocket, e);
    }

    private static void ReceiveCompleted(object? sender, SocketAsyncEventArgs e)
    {
        // Receive 작업을 수행한 클라이언트 소켓
        var clientSocket = (Socket)sender!;
        
        // BytesTransferred < 1
        //  1. 정상적인 TCP 종료(상대가 연결 종료)
        //  2. 소켓 오류
        // 여기서는 클라이언트 종료로 처리
        if (e.BytesTransferred < 1)
        {
            Console.WriteLine("Client Disconnected");
            
            // 더 이상 통신하지 않으므로 소켓 정리
            clientSocket.Dispose();
            
            // Receive 컨텍스트(SocketAsyncEventArgs)도 더 이상 필요 없음
            e.Dispose();
            return;
        }

        // 수신된 데이터 처리
        // Buffer에는 이전 설정한 버퍼가 들어 있음
        if (e.Buffer != null)
        {
            Console.WriteLine(Encoding.UTF8.GetString(e.Buffer));

            // 다음 Receive를 대비해 버퍼 초기화
            //  (필수는 아니며, BytesTransferred로 유효 범위만 관리해도 된다)
            Array.Clear(e.Buffer);
        }
        
        // 다시 같은 클라이언트에 대해 Receive 등록
        // 이벤트 기반(IOCP) 비동기 처리
        var bReceivePending = clientSocket.ReceiveAsync(e);
        
        // Receive가 즉시 완료된 경우
        // Completed 이벤트가 호출되지 않으므로 직접 콜백 호출
        if (!bReceivePending)
            ReceiveCompleted(clientSocket, e);
    }
}