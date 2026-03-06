using Common.Types;

namespace Common.Web;

public interface IKeyValueStore
{
    #region Core
    public Task<bool> AddHashAsync(string key, string hashField, string hashValue);
    public Task<bool> AddSortedSetAsync(string key, string member, double score);
    public Task<RankingEntry[]> GetTopRankingByType(string key, int rank);
    public Task<long?> GetMemberRank(string key, string member);
    public Task<double?> GetMemberScore(string key, string member);
    #endregion
    
    #region Session
    public Task<bool> AddSessionInfoAsync(string key, string value);
    public Task<string> GetSessionInfoAsync(string key);
    #endregion
}