using System.Text.Json;
using Shared.Constants;
using Shared.GameData;
using Shared.Types;

namespace Common.Web;

public static class ResponseResultTable
{
    private static readonly 
        Dictionary<EResponseResult, 
            (bool IsSuccess, Dictionary<ELanguage, string> Messages)> 
        mResponseTable = [];
    
    public static 
        (bool IsSuccess, Dictionary<ELanguage, string> Messages)
        GetStatusDefinition(EResponseResult status) => mResponseTable[status];
    
    public static void Init(string path)
    {
        var jsonText = File.ReadAllText(path);

        var data = JsonSerializer.Deserialize<ResponseResult>(jsonText);

        if (data == null || data.Data.Count == 0)
            throw new NullReferenceException(ExceptionMessage.EMPTY_RESPONSE_STATUS);

        foreach (var result in data.Data)
        {
            var status = (EResponseResult)result.Code;
            var messageList = result.Messages;
            
            var messageDic = new Dictionary<ELanguage, string>
            {
                { ELanguage.Ko, messageList[0] },
                { ELanguage.En, messageList[1] }
            };

            mResponseTable.Add(status, (result.IsSuccess, messageDic));
        }
        
        var values = Enum.GetValues<EResponseResult>();
        
        foreach (var value in values)
        {
            if (!mResponseTable.TryGetValue(value, out var result))
                throw new InvalidOperationException($"Not set up status - {value}({(int)value})");
        }
    }
}