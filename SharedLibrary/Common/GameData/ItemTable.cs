using Common.Manager;
using Shared.GameData;

namespace Common.GameData;

public class ItemTable : BaseTable<Item>
{
    public ItemTable(string tableName) : base(tableName)
    { }

    public override bool Load(string path)
    {
        var data = GameDataManager.LoadGameData<Item>(path, TableName);

        mTable.Clear();

        foreach (var info in data.Data)
        {
            mTable[info.ItemId] = info;
        }

        return true;
    }

    public override bool Validate()
    {
        return true;
    }
}