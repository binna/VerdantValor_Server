using Common.Error;
using Common.GameData;
using Common.GameData.Tables;

namespace Common.Manager;

public class GameDataManager : ITableRegistry
{
    public static ResponseResultTable ResponseResultTable { get; } = new("ResponseResultTable");
    public static ItemTable ItemTable { get; } = new("ItemTable");
    public static StoreTable StoreTable { get; } = new("StoreTable");

    private static readonly IBaseTable[] mAllTables =
    [
        ResponseResultTable,
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

            throw new InvalidOperationException(
                string.Format(ErrorMessages.TABLE_NOT_FOUND, typeof(T).Name));
        }
    
        return null;
    }
}