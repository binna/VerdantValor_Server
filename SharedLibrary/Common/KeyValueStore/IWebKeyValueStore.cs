using Common.Types;

namespace Common.KeyValueStore;

public interface IWebKeyValueStore
{
    public Task<bool> AddHashAsync(string key, string hashField, string hashValue);
    public Task<bool> AddSortedSetAsync(string key, string member, double score);
    public Task<RankingEntry[]> GetTopRankingByType(string key, int rank);
    public Task<long?> GetMemberRank(string key, string member);
    public Task<double?> GetMemberScore(string key, string member);
}