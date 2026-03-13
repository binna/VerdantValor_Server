using System.Text.Json;
using Common.Constants;
using Common.GameData;
using Shared.GameData;

namespace Common.Manager;

public class GameDataManager
{
    public static ResponseResultTable ResponseResultTable { get; }  = new("ResponseResult");
    public static ItemTable ItemTable { get; } = new("Item");
    public static StoreTable StoreTable { get; } = new("Store");
    
   public static void LoadAllGameData(string path)
   {
       ResponseResultTable.Load(path);
       ResponseResultTable.Validate();
       
       ItemTable.Load(path);
       ItemTable.Validate();
       
       StoreTable.Load(path);
       StoreTable.Validate();
   }
    
    public static GameData<T> LoadGameData<T>(string path, string filename)
    {
        var jsonText = File.ReadAllText($"{path}/{filename}.json");
        var data = JsonSerializer.Deserialize<GameData<T>>(jsonText);
        
        if (data == null || data.Data.Count == 0)
            throw new InvalidDataException(
                string.Format(ErrorMessages.FAILED_TO_LOAD_FILE, $"{filename}"));

        return data;
    }
}