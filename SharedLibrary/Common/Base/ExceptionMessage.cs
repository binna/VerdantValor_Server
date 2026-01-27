namespace Common.Base;

public static class ExceptionMessage
{
    public const string EMPTY_HTTP_CONTEXT
        = "HttpContext is null.";
    
    public const string EMPTY_RESPONSE_STATUS 
        = "ResponseStatus.json contains no valid data.";

    public const string INVALID_DATETIME_FORMAT =
        "Invalid datetime format(Expected format: yyyy-MM-dd HH:mm:ss zzz)";
}