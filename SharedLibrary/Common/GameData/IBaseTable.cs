namespace Common.GameData;

public interface IBaseTable
{
    public string TableName { get; }
    public bool Load(string path);
    public void Validate();
    public void CrossValidate(ITableRegistry registry);
}