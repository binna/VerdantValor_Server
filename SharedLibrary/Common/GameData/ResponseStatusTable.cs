using System.Text.Json;
using Common.Base;
using VerdantValorShared.Common.Web;
using VerdantValorShared.GameData;

namespace Common.GameData;

public static class ResponseStatusTable
{
    private static readonly 
        Dictionary<AppEnum.EResponseStatus, 
            (bool IsSuccess, Dictionary<AppEnum.ELanguage, string> Messages)> 
        mResponseTable = [];
    
    public static 
        (bool IsSuccess, Dictionary<AppEnum.ELanguage, string> Messages)
        GetStatusDefinition(AppEnum.EResponseStatus status) => mResponseTable[status];
    
    public static void Init(string path)
    {
        var jsonText = File.ReadAllText(path);

        var data = JsonSerializer.Deserialize<ResponseStatus>(jsonText);

        if (data == null || data.Data.Count == 0)
            throw new NullReferenceException(ExceptionMessage.EMPTY_RESPONSE_STATUS);

        foreach (var item in data.Data)
        {
            var status = (AppEnum.EResponseStatus)item.Code;
            var messageList = item.Message;
            
            var messageDic = new Dictionary<AppEnum.ELanguage, string>
            {
                { AppEnum.ELanguage.Ko, messageList[0] },
                { AppEnum.ELanguage.En, messageList[1] }
            };

            mResponseTable.Add(status, (item.IsSuccess, messageDic));
        }
        
        var values = Enum.GetValues<AppEnum.EResponseStatus>();
        foreach (var value in values)
        {
            if (!mResponseTable.TryGetValue(value, out var result))
                throw new InvalidOperationException($"Not set up status - {value}({(int)value})");
        }
    }
}