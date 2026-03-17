using Shared.GameData;

namespace Common.GameData.Tables;

public class ItemTable : BaseTable<Item>
{
    public ItemTable(string tableName) : base(tableName)
    { }

    public override bool Load(string path)
    {
        var data = LoadFromJson<Item>(path, TableName);
        
        mTable.Clear();
        
        foreach (var item in data.Data)
        {
            mTable[item.Id] = item;
        }

        return true;
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