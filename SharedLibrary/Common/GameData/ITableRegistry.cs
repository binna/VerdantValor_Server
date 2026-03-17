namespace Common.GameData;

public interface ITableRegistry
{
    public T GetTable<T>() where T : class, IBaseTable;
}