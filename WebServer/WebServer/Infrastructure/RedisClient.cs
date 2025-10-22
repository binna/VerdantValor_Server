using StackExchange.Redis;

namespace WebServer.Configs
{
    public class RedisClient
    {
        private readonly ILogger<RedisClient> logger;
        private readonly IDatabase database;

        public RedisClient(ILogger<RedisClient> logger, 
                           IConfiguration configuration)
        {
            this.logger = logger;

            var endpoint = $"{configuration["DB:Redis:Host"]}:{configuration["DB:Redis:Port"]}";
            var connection = ConnectionMultiplexer.Connect(endpoint);
            this.database = connection.GetDatabase();

            if (database == null)
            {
                logger.LogError("Redis Connection Fail");
                // TODO
            }
        }

        public Task<bool> SetStringValueAsync(string key, string value)
        {
            return database.StringSetAsync(key, value);
        }

        public Task<bool> SetHashFieldValueAsync(string key, string hashField, string hashValue)
        {
            return database.HashSetAsync(key, hashField, hashValue);
        }
    }
}
