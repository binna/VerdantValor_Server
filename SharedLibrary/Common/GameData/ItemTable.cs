using Shared.GameData;

namespace Common.GameData;

public class ItemTable : IGameDataTable<Item>
{
    private static readonly Dictionary<int, Item> mTable = [];
    
    public static bool TryGet(int id, out Item data)
    {
        return mTable.TryGetValue(id, out data);
    }

    public static void Load(string path)
    {
        // TODO
    }
}