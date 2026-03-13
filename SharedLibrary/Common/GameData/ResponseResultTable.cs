using Common.Constants;
using Common.Manager;
using Shared.GameData;
using Shared.Types;

namespace Common.GameData;

public class ResponseResultTable : BaseTable<ResponseResult>
{
    private Dictionary<EResponseResult, (bool IsSuccess, ulong TextId)> mResultMap = [];

    public ResponseResultTable(string tableName) : base(tableName)
    { }
    
    public bool TryGet(EResponseResult id, out (bool IsSuccess, ulong TextId) responseResult)
    {
        return mResultMap.TryGetValue(id, out responseResult);
    }

    public override bool Load(string path)
    {
        var data = GameDataManager.LoadGameData<ResponseResult>(path, TableName);

        mResultMap.Clear();
            
        foreach (var info in data.Data)
        {
            mResultMap[(EResponseResult)info.Code] = (info.IsSuccess, info.TextId);
        }

        return true;
    }

    public override bool Validate()
    {
        foreach (var code in Enum.GetValues<EResponseResult>())
        {
            if (code == EResponseResult.None)
                continue;

            if (!mResultMap.TryGetValue(code, out _))
                throw new InvalidOperationException(
                    string.Format(ErrorMessages.RESPONSE_RESULT_NOT_SET, $"{code}"));
        }

        return true;
    }
}