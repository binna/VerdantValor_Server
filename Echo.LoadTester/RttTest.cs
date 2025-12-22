using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Echo.LoadTester;

public class RttTest
{
    public static void Start(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endPoint = new IPEndPoint(IPAddress.Loopback, 20000);
            long startTime;
        
            socket.Connect(endPoint);
        
            {
                var data = $"{Guid.NewGuid()}";
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

                startTime = Stopwatch.GetTimestamp();
                socket.Send(messageBuffer);
            }

            {
                var headerBuffer = new byte[sizeof(short)];
                var headerLength = socket.Receive(headerBuffer);
                var rttMs =
                    (Stopwatch.GetTimestamp() - startTime) * 1000.0 / Stopwatch.Frequency;

                Console.WriteLine($"RTT: {rttMs:F3} ms");

                if (headerLength == 0)
                    return;

                if (headerLength == 1)
                    socket.Receive(headerBuffer, 1, 1, SocketFlags.None);
            
                var receiveDataLength = 
                    IPAddress.NetworkToHostOrder(BitConverter.ToInt16(headerBuffer));
            
                var receiveDataBuffer = new byte[receiveDataLength];

                var totalSize = 0;

                while (totalSize < receiveDataLength)
                {
                    var readLength = socket.Receive(
                        receiveDataBuffer, totalSize, receiveDataLength - totalSize, SocketFlags.None);
                
                    if (readLength == 0)
                        return;
                
                    totalSize += readLength;
                }

                var receiveData = Encoding.UTF8.GetString(receiveDataBuffer);
            }
        }
        
        Console.ReadKey();
    }
}