using Common.Manager;
using Shared.GameData;

namespace Common.GameData;

public class StoreTable : BaseTable<Store>
{
    public StoreTable(string tableName) : base(tableName)
    { }

    public override bool Load(string path)
    {
        var data = GameDataManager.LoadGameData<Store>(path, TableName);

        mTable.Clear();

        foreach (var info in data.Data)
        {
            mTable[info.StoreId] = info;
        }

        return true;
    }

    public override bool Validate()
    {
        return true;
    }
}