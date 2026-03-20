using Common.GameData;
using Common.GameData.Tables;

namespace Common.Manager;

public class GameDataManager : ITableRegistry
{
    public static ResponseResultTable ResponseResultTable { get; } = new("ResponseResult");
    public static BannedWordTable BannedWordTable { get; } = new("BannedWord");
    public static ItemTable ItemTable { get; } = new("Item");
    public static StoreTable StoreTable { get; } = new("Store");

    private static readonly IBaseTable[] mAllTables =
    [
        ResponseResultTable,
        BannedWordTable,
        ItemTable,
        StoreTable
    ];
    
    public static void LoadAllGameData(string path)
    {
        var registry = new GameDataManager();
        foreach (var table in mAllTables)
        {
            table.Load(path);
        }
        
        foreach (var table in mAllTables)
        {
            table.CrossValidate(registry);
        }
    }
    
    public T GetTable<T>() where T : class, IBaseTable
    {
        foreach (var table in mAllTables)
        {
            if (table is T t)
                return t;
        }
        
        return null;
    }
}