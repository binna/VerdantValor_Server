using System.Net;
using System.Net.Sockets;
using System.Text;

const int CLIENT_MAX = 100;

// Parallel.For: 동시에 여러 스레드를 돌려서 병렬 실행하는 for 문 -> 동기

List<Task> tasks = [];

for (var i = 0; i < CLIENT_MAX; i++)
{
    var clientNum = i;
    var newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    //var endPoint = new IPEndPoint(IPAddress.Loopback, 20000);
    var endPoint = new IPEndPoint(IPAddress.Parse("211.46.62.188"), 20000);
    newSocket.Connect(endPoint);

    Console.WriteLine($"{clientNum} 연결");
    
    tasks.Add(Task.Run(async () =>
    {
        // 람다는
        //  바깥 로컬 변수를 값이 아니라 변수 자체(저장 위치)를 캡처한다
        //  그래서 나중에 실행될 때도 같은 변수가 바뀐 값을 보게 된다
        
        // IDE의 임시 변수 인라인화 제안은
        //  이 변수를 한 번만 쓰니 newSocket을 바로 써도 된다는 코드 스타일 추천일 뿐,
        //  람다 캡처에 따른 버그까지는 고려하지 않는다
        
        // 각 Task가 Accept 시점의 소켓과 클라 번호를 독립적으로 가져야 하므로
        //  람다 안에서 newSocket, clientNum을 한 번 복사해 둔다
        var socket = newSocket;
        var num = clientNum;
        
        while (true)
        {
            var data = $"{Guid.NewGuid()}";
            
            Console.WriteLine($"{num} 클라 전송: {data}");

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

            await Task.Delay(new Random().Next(1000, 15000));
        }
    }));

    tasks.Add(Task.Run(async () =>
    {
        var socket = newSocket;
        var num = clientNum;
        
        while (true)
        {
            var headerBuffer = new byte[sizeof(short)];
            var headerLength = newSocket.Receive(headerBuffer);

            if (headerLength == 0)
            {
                Console.WriteLine("클라: 서버 연결 종료");
                return;
            }

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
                {
                    Console.WriteLine("클라: 서버 연결 종료");
                    return;
                }
        
                totalSize += readLength;
            }

            var receiveData = Encoding.UTF8.GetString(receiveDataBuffer);
    
            Console.WriteLine($"{num} 클라 받음: {receiveData}");
            
            await Task.Delay(1000);
        }
    }));
}

await Task.WhenAll(tasks);