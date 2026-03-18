using Shared.GameData;

namespace Common.GameData.Tables;

public class BannedWordTable : BaseTable<BannedWord>
{
    private static HashSet<string> mBannedSet = [];
    
    public BannedWordTable(string tableName) : base(tableName)
    { }
    
    public static bool ContainsBannedWord(string text)
    {
        return mBannedSet.Any(word => 
            text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    public override void Load(string path)
    {
        var data = LoadFromJson<BannedWord>(path, TableName);
        
        mTable.Clear();
        
        foreach (var bannedWord in data.Data)
        {
            mBannedSet.Add(bannedWord.Text);
        }
    }

    public override void Validate()
    {
        // 별도의 검증 로직이 필요 없음
    }

    public override void CrossValidate(ITableRegistry registry)
    {
        // 별도의 검증 로직이 필요 없음
    }
}