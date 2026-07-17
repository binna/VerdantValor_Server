namespace Common.GameData;

public interface ITableRegistry
{
    T GetTable<T>() where T : class, IBaseTable;
}