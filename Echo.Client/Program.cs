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
    var data = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(data))
        continue;         

    if (data.Equals("exit", StringComparison.OrdinalIgnoreCase))
        return;

    // TCP는 메시지 경계를 제공하지 않기 때문에
    // 먼저 전체 메시지의 길이를 보내고 그 다음 실제 데이터를 보낸다
    // 수신자는 보내준 메시지 길이를 기준으로 메시지를 읽어 하나의 패킷으로 재구성한다
    // [메시지 길이] + [메시지 데이터]
    
    var dataBuffer = Encoding.UTF8.GetBytes(data);
    
    // 우리가 사용하는 대부분의 서버는 리틀 엔디안이고,
    // 네트워크 프로토콜은 빅 엔디안을 사용하므로, 메시지 길이를 전송하기 전에 네트워크 표준으로 변환
    var headerBuffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)dataBuffer.Length));
    
    var messageBuffer = new byte[headerBuffer.Length + dataBuffer.Length];
    
    Array.Copy(
        headerBuffer, 0, 
        messageBuffer, 0, 
        headerBuffer.Length);
    Array.Copy(
        dataBuffer, 0, 
        messageBuffer, headerBuffer.Length, 
        dataBuffer.Length);
    
    clientSocket.Send(messageBuffer);

    var receiveHeaderBuffer = new byte[sizeof(short)];
    var receiveHeaderLength = clientSocket.Receive(receiveHeaderBuffer);

    if (receiveHeaderLength == 1)
        clientSocket.Receive(receiveHeaderBuffer, 1, 1, SocketFlags.None);
    
    var receiveDataLength = 
        IPAddress.HostToNetworkOrder(BitConverter.ToInt16(receiveHeaderBuffer));
    
    var receiveDataBuffer = new byte[receiveDataLength];

    var totalSize = 0;

    while (totalSize < receiveDataLength)
    {
        var readLength = clientSocket.Receive(
            receiveDataBuffer, totalSize, receiveDataLength - totalSize, SocketFlags.None);
        
        totalSize += readLength;
    }

    var receiveData = Encoding.UTF8.GetString(receiveDataBuffer);
    
    Console.WriteLine($"받음 : {receiveData}");
}