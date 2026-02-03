using Shared.Types;

namespace Common.Web;

public class ApiResponse(bool isSuccess, int code, string message)
{
    public bool IsSuccess { get; } = isSuccess;
    public int Code { get; } = code;
    public string Message { get; } = message;
    
    public static ApiResponse From(EResponseResult status, ELanguage language)
    {
        var statusDefinition = ResponseResultTable.GetStatusDefinition(status);
        
        return new ApiResponse(
            statusDefinition.IsSuccess, 
            (int)status, 
            statusDefinition.Messages[language]);
    }
}

public class ApiResponse<T>(bool isSuccess, int code, string message, T? result = default)
    : ApiResponse(isSuccess, code, message)
{
    public T? Result { get; } = result;
    
    public static ApiResponse<T> From(EResponseResult status, ELanguage language, T? result = default)
    {
        var statusDefinition = ResponseResultTable.GetStatusDefinition(status);
        
        return new ApiResponse<T>(
            statusDefinition.IsSuccess, 
            (int)status, 
            statusDefinition.Messages[language], result);
    }
}