using System.Net;
using System.Net.Sockets;
using System.Text;

const int CLIENT_MAX = 10000;

// Parallel.For: 동시에 여러 스레드를 돌려서 병렬 실행하는 for 문
Parallel.For(0, CLIENT_MAX, i =>
{
    using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    //var endPoint = new IPEndPoint(IPAddress.Loopback, 20000);
    var endPoint = new IPEndPoint(IPAddress.Parse("211.46.62.188"), 20000);
    socket.Connect(endPoint);

    while (true)
    {
        var data =
            $"{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}";
        
        var headerBuffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)data.Length));
        var dataBuffer = Encoding.UTF8.GetBytes(data);
        var messageBuffer = new byte[headerBuffer.Length + dataBuffer.Length];
        
        Array.Copy(
            headerBuffer, 0,
            messageBuffer, 0,
            headerBuffer.Length);
        Array.Copy(
            dataBuffer, 0,
            messageBuffer, headerBuffer.Length,
            dataBuffer.Length);
        
        socket.Send(messageBuffer);
        
        Task.Delay(new Random().Next(1000, 1500));
    }
});