using System.Net;
using System.Net.Sockets;
using System.Text;

using var serverSocket =
    new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

var endPoint = 
    new IPEndPoint(IPAddress.Loopback, 20000);

// 서버 소켓에 ip, port 할당
serverSocket.Bind(endPoint);

Console.WriteLine("서버: Listen 시작");

// 클라이언트들의 연결 요청을 대기하는 상태로 만듦
// 백로그큐 = 클라이언트들의 연결 요청 대기실
// 20 : 백로그큐 Size로 보면 됨
serverSocket.Listen(20);

Console.WriteLine("서버: Accept 대기");

// 백로그큐에서 하나 꺼내와서 연결 요청을 수락
// 클라이언트와 데이터 통신을 위해 소켓 만듦
using var clientSocket = serverSocket.Accept();
Console.WriteLine("서버: 클라 접속됨");
Console.WriteLine($"연결됨: {clientSocket.RemoteEndPoint}");

while (true)
{
    var buffer = new byte[256];
    var totalBytes = clientSocket.Receive(buffer);

    if (totalBytes < 1)
    {
        Console.WriteLine("클라이언트의 종료");
        return;
    }

    var str = Encoding.UTF8.GetString(buffer);
    Console.WriteLine(str);
    
    clientSocket.Send(buffer);
}