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
    var headerBuffer = new byte[sizeof(short)];
    var headerLength = clientSocket.Receive(headerBuffer);

    // SocketFlags.None
    //  기본 모드로 보내라/받아라
    if (headerLength == 1)
        clientSocket.Receive(headerBuffer, 1, 1, SocketFlags.None);
    
    var dataLength = 
        IPAddress.HostToNetworkOrder(BitConverter.ToInt16(headerBuffer));
    
    var dataBuffer = new byte[dataLength];

    var totalSize = 0;

    while (totalSize < dataLength)
    {
        // 주의!!
        //  Receive의 size 파라미터(세 번째 인자)는
        //  최대로 읽을 수 있는 길이를 의미할 뿐
        //  반드시 그만큼 읽는다는 의미가 아니다
        var readLength = clientSocket.Receive(
            dataBuffer, totalSize, dataLength - totalSize, SocketFlags.None);
        
        totalSize += readLength;
    }

    var data = Encoding.UTF8.GetString(dataBuffer);
    
    if (string.IsNullOrWhiteSpace(data))
        continue;         

    if (data.Equals("exit", StringComparison.OrdinalIgnoreCase))
        return;
    
    Console.WriteLine(data);

    var messageBuffer = new byte[headerBuffer.Length + dataLength];
    
    Array.Copy(
        headerBuffer, 0, 
        messageBuffer, 0, 
        headerBuffer.Length);
    Array.Copy(
        dataBuffer, 0, 
        messageBuffer, headerBuffer.Length, 
        dataBuffer.Length);
    
    clientSocket.Send(messageBuffer);
}