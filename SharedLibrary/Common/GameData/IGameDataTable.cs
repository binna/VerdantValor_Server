namespace Common.GameData;

public interface IGameDataTable<T>
{
    public static abstract bool TryGet(int id, out T data);
    public static abstract void Load(string path);
}