using Shared.GameData;

namespace Common.GameData;

public class StoreTable : IGameDataTable<Store>
{
    private static readonly Dictionary<int, Store> mTable = [];

    public static bool TryGet(int id, out Store data)
    {
        return mTable.TryGetValue(id, out data);
    }

    public static void Load(string path)
    {
        // TODO
    }
}