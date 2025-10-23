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

        public Task<bool> AddStringAsync(string key, string value)
        {
            return database.StringSetAsync(key, value);
        }

        public Task<bool> AddHashAsync(string key, string hashField, string hashValue)
        {
            return database.HashSetAsync(key, hashField, hashValue);
        }

        public Task<bool> AddSortedSetAsync(string key, string member, double score)
        {
            return database.SortedSetAddAsync(key, member, score);
        }

        public Task<SortedSetEntry[]> GetMemberTopRanking(string key, int rank)
        {
            return database.SortedSetRangeByRankWithScoresAsync(key, 0, rank - 1, Order.Descending);
        }

        public Task<long?> GetMemberRank(string key, string info)
        {
            return database.SortedSetRankAsync(key, info, Order.Descending);
        }
    }
}