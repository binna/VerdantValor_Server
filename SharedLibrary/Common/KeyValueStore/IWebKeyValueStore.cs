using Common.Types;

namespace Common.KeyValueStore;

public interface IWebKeyValueStore
{
    Task<bool> AddHashAsync(string key, string hashField, string hashValue);
    Task<bool> AddSortedSetAsync(string key, string member, double score);
    Task<RankingEntry[]> GetTopRankingByType(string key, int rank);
    Task<long?> GetMemberRank(string key, string member);
    Task<double?> GetMemberScore(string key, string member);
}