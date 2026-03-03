using System.Text.Json;
using Common.Web;
using Shared.GameData;
using Shared.Types;

namespace Common.Manager;

public class GameDataManager
{
    private static Dictionary<int, Item> mItemTable = [];
    private static Dictionary<int, Store> mStoreTable = [];
    private static Dictionary<EResponseResult, (bool IsSuccess, ulong TextId)> mResponseResultTable = [];

    private static bool mIsLoaded;
    
    public static bool TryItemGet(int id, out Item item)
    {
        return mItemTable.TryGetValue(id, out item);
    }
    
    public static bool TryStoreGet(int id, out Store store)
    {
        return mStoreTable.TryGetValue(id, out store);
    }
    
    public static bool TryResponseResultGet(EResponseResult id, out (bool IsSuccess, ulong TextId) responseResult)
    {
        return mResponseResultTable.TryGetValue(id, out responseResult);
    }

    public static void LoadAllGameData(string path)
    {
        if (mIsLoaded)
            return;

        mIsLoaded = true;
        
        LoadGameData<ResponseResult>(path, "ResponseResult.json", data =>
        {
            foreach (var info in data)
            {
                mResponseResultTable.Add((EResponseResult)info.Code, (info.IsSuccess, info.TextId));
            }
            
            foreach (var code in Enum.GetValues<EResponseResult>())
            {
                if (code == EResponseResult.None) 
                    continue;
                
                if (!mResponseResultTable.TryGetValue(code, out _))
                    throw new InvalidOperationException(
                        string.Format(ExceptionMessage.RESPONSE_RESULT_NOT_SET, $"{code}", $"{(int)code}"));
            }
        });
        
        LoadGameData<Item>(path, "Item.json", data =>
        {
            foreach (var info in data)
            {
                mItemTable.Add(info.ItemId, info);
            }
        });
        
        LoadGameData<Store>(path, "Store.json", data =>
        {
            foreach (var info in data)
            {
                mStoreTable.Add(info.StoreId, info);
            }
        });
    }
    
    private static void LoadGameData<T>(string path, string filename, Action<List<T>> apply)
    {
        var jsonText = File.ReadAllText($"{path}/{filename}");
        var data = JsonSerializer.Deserialize<GameData<T>>(jsonText);
        
        if (data == null || data.Data.Count == 0)
            throw new InvalidDataException(
                string.Format(ExceptionMessage.FAILED_TO_LOAD_FILE, $"{filename}"));

        apply(data.Data);
    }
}