using Common.Manager;
using Shared.Types;

namespace Common.Web;

public class ApiResponse(bool isSuccess, int code, ulong textId)
{
    public bool IsSuccess { get; } = isSuccess;
    public int Code { get; } = code;
    public ulong TextId { get; } = textId;
    
    public static ApiResponse From(EResponseResult code)
    {
        if (!GameDataManager.TryResponseResultGet(code, out var responseResult))
            throw new InvalidOperationException(
                string.Format(ExceptionMessage.RESPONSE_RESULT_NOT_SET, $"{code}", $"{(int)code}"));
        
        return new ApiResponse(
            responseResult.IsSuccess, 
            (int)code,
            responseResult.TextId);
    }
}

public class ApiResponse<T>(bool isSuccess, int code, ulong textId, T result)
    : ApiResponse(isSuccess, code, textId)
{
    public T Result { get; } = result;
    
    public static ApiResponse<T> From(EResponseResult code, T result = default)
    {
        var baseResponse = ApiResponse.From(code);
        
        return new ApiResponse<T>(
            baseResponse.IsSuccess,
            (int)code,
            baseResponse.TextId,
            result);
    }
}