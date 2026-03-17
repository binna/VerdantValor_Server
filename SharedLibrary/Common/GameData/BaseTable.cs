using System.Text.Json;
using Common.Error;
using Shared.GameData;

namespace Common.GameData;

public abstract class BaseTable<TData> : IBaseTable
{
    public string TableName { get; }
    
    protected Dictionary<int, TData> mTable = new();

    protected BaseTable(string tableName)
    {
        TableName = tableName;
    }

    public abstract bool Load(string path);
    public abstract void Validate();
    public abstract void CrossValidate(ITableRegistry registry);
    
    protected static GameData<T> LoadFromJson<T>(string path, string filename)
    {
        var jsonText = File.ReadAllText($"{path}/{filename}.json");
        var data = JsonSerializer.Deserialize<GameData<T>>(jsonText);
        
        if (data == null || data.Data.Count == 0)
            throw new InvalidDataException(
                string.Format(ErrorMessages.FAILED_TO_LOAD_FILE, $"{filename}"));

        return data;
    }
    
    public bool TryGet(int id, out TData value)
    {
        return mTable.TryGetValue(id, out value);
    }
}