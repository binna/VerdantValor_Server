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

            var host = configuration["DB:Redis:Host"];
            var port = configuration["DB:Redis:Port"];

            if (host == null || port == null)
            {
                logger.LogCritical("Redis configuration is missing required fields");
                Environment.Exit(1);
            }

            var endpoint = $"{host}:{port}";
            var connection = ConnectionMultiplexer.Connect(endpoint);

            this.database = connection.GetDatabase(0);

            if (database == null)
            {
                logger.LogCritical("Redis Connection Fail");
                Environment.Exit(1);
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

        public Task<SortedSetEntry[]> GetTopRankingByType(string key, int rank)
        {
            return database.SortedSetRangeByRankWithScoresAsync(key, 0, rank - 1, Order.Descending);
        }

        public Task<long?> GetMemberRank(string key, string member)
        {
            return database.SortedSetRankAsync(key, member, Order.Descending);
        }

        public Task<double?> GetMemberScore(string key, string member)
        {
            return database.SortedSetScoreAsync(key, member);
        }
    }
}