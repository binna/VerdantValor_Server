namespace Common.GameData;

public interface IBaseTable
{
    string TableName { get; }
    void Load(string path);
    void Validate();
    void CrossValidate(ITableRegistry registry);
}