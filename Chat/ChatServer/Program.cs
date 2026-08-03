using System.Net;
using System.Text.Json;
using Ado;
using Ado.Daos;
using Common;
using Common.KeyValueStore;
using Redis;
using Tcp;

namespace ChatServer;

internal class Server
{
    static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();
        
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var jsonText = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<Config>(jsonText) ?? throw new Exception("Invalid Configuration File");
        
        IChatPartyDao chatPartyDao = new ChatPartyDao(new DbFactory(config.Database.Url));
        ISessionKeyValueStore sessionKeyValueStore = new SessionKeyValueStore(new RedisCacheDriver(config.Redis.Host, $"{config.Redis.Port}", ShareServerConst.USER_SESSION_DB_NUM), 0);
        var worldPartyManager = new WorldPartyManager(chatPartyDao, sessionKeyValueStore, cts.Token);
        var sessionManager = new SessionManager(chatPartyDao, sessionKeyValueStore, cts.Token);
        
        var server = new ChatSocketServer(chatPartyDao, sessionKeyValueStore, worldPartyManager, sessionManager, config, cts);
        await server.StartAsync(IPAddress.Any, 20000);
    }
}
