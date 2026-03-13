namespace Common.GameData;

public abstract class BaseTable<TData> : IBaseTable
{
    protected Dictionary<int, TData> mTable = new();
    
    public string TableName { get; }

    protected BaseTable(string tableName)
    {
        TableName = tableName;
    }

    public bool TryGet(int id, out TData value)
    {
        return mTable.TryGetValue(id, out value);
    }

    public abstract bool Load(string path);
    public abstract bool Validate();
}
