using System.Net;
using System.Net.Sockets;
using System.Text;

using var clientSocket =
    new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

var endPoint = 
    new IPEndPoint(IPAddress.Loopback, 20000);

clientSocket.Connect(endPoint);
Console.WriteLine("클라: 연결 성공");

while (true)
{
    var str = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(str))
        return;

    if (str.Equals("exit", StringComparison.OrdinalIgnoreCase))
        return;

    var buffer = Encoding.UTF8.GetBytes(str);
    clientSocket.Send(buffer);

    var buffer2 = new byte[256];
    var bytesRead = clientSocket.Receive(buffer2);
    
    if (bytesRead < 1)
    {
        Console.WriteLine("서버의 연결 종료");
        return;
    }
    
    var str2 = Encoding.UTF8.GetString(buffer2);
    Console.WriteLine($"받음 : {str2}");
}