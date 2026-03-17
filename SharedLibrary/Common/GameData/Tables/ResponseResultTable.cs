using Common.Error;
using Shared.GameData;
using Shared.Types;

namespace Common.GameData.Tables;

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
        var data = LoadFromJson<ResponseResult>(path, TableName);

        mResultMap.Clear();
            
        foreach (var info in data.Data)
        {
            mResultMap[(EResponseResult)info.Code] = (info.IsSuccess, info.TextId);
        }

        return true;
    }

    public override void Validate()
    {
        var errors = new List<ValidationError>();
        
        foreach (var code in Enum.GetValues<EResponseResult>())
        {
            if (code == EResponseResult.None)
                continue;

            if (!mResultMap.TryGetValue(code, out _))
            {
                errors.Add(new ValidationError(
                    context: $"ResponseResult={code}",
                    field: nameof(EResponseResult),
                    type: ValidationError.ValidationErrorType.NotFound,
                    message: "Result code not found."));
            }
        }
        
        if (errors.Count > 0)
        {
            var errorText = string.Join(
                Environment.NewLine,
                errors.Select(x => x.ToString()));

            throw new InvalidOperationException(
                $"ResponseResultTable Validation failed.\n{Environment.NewLine}{errorText}");
        }
    }

    public override void CrossValidate(ITableRegistry registry)
    {
        // 별도의 검증 로직이 필요 없음
    }
}