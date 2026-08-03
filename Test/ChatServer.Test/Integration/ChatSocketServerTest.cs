using System.Net;
using System.Net.Sockets;
using Ado.Daos;
using Common.KeyValueStore;
using NSubstitute;
using Tcp;
using Xunit.Abstractions;

namespace ChatServer.Test.Integration;

public class ChatSocketServerTest
{
    private readonly ITestOutputHelper mOutput;
    private readonly Config mConfig;
    private readonly IChatPartyDao mChatPartyDao;
    private readonly ISessionKeyValueStore mSessionKeyValueStore;

    public ChatSocketServerTest(ITestOutputHelper output)
    {
        mOutput = output;

        mChatPartyDao = Substitute.For<IChatPartyDao>();
        mChatPartyDao.FindAllPartyIdAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        mChatPartyDao.FindPartyIdByMemberUserIdAsync(Arg.Any<ulong>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        mSessionKeyValueStore = Substitute.For<ISessionKeyValueStore>();
        mSessionKeyValueStore.RefreshServerHeartbeatAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);

        mConfig = Substitute.For<Config>();
    }

    
    [Theory]
    [InlineData(1000)]
    public async Task Test_여러_클라이언트_동시_접속시_전부_정상_연결됨(int clientCount)
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var worldPartyManager = new WorldPartyManager(mChatPartyDao, mSessionKeyValueStore, token);
        var sessionManager = new SessionManager(mChatPartyDao, mSessionKeyValueStore, token);

        using var server = new ChatSocketServer(
            mChatPartyDao, mSessionKeyValueStore, worldPartyManager, sessionManager, mConfig, cts);
        
        var startAsync = server.StartAsync(IPAddress.Any, 20000);

        await Task.Delay(1000, token);

        List<Task<TcpClient?>> clients = [];
        var clientConnectedCnt = 0;
        
        for (var i = 0; i < clientCount; i++)
        {
            clients.Add(Task.Run(async () =>
            {
                // 대량의 클라이언트가 동시에 접속을 시도하면 일부가 연결 거부로 실패함
                //  이는 서버 코드의 문제가 아니라, 
                //  로컬 환경 OS의 TCP backlog(소켓 큐) 한계 때문에 발생하는 현상으로,
                //  실행마다 성공과 실패 개수가 달라진다.
                //  추후 클라이언트에서도 실패 시 재시도할 예정이라,
                //  미리 재시도 로직을 구현해 결국 모두 연결되는지를 검증한다.
                for (var retry = 0; retry < 50; retry++)
                {
                    try
                    {
                        var client = new TcpClient();
                        await client.ConnectAsync(IPAddress.Loopback, 20000, token);
                        Interlocked.Increment(ref clientConnectedCnt);
                        return client;
                    }
                    catch (Exception ex)
                    {
                        mOutput.WriteLine($"[Test] Connect Failed (retry {retry}): {ex.Message}");
                        await Task.Delay(200, token); // 짧은 간격으로 재시도
                    }
                }
            
                mOutput.WriteLine("[Test] Max retry exceeded, giving up.");
                return null;
            }, token));
        }
        
        await Task.WhenAll(clients);
        
        Assert.Equal(clientCount, clientConnectedCnt);
        
        foreach (var client in clients)
        {
            Assert.NotNull(client.Result);
            client.Result?.Dispose();    
        }

        await cts.CancelAsync();
    }
}