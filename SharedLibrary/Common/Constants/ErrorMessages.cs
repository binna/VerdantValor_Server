namespace Common.Constants;

public static class ErrorMessages
{
    public const string FAILED_TO_LOAD_FILE = 
        "Failed to load {0}.";

    public const string RESPONSE_RESULT_NOT_SET = 
        "Status not set up: {0}.";
    
    public const string INVALID_DATETIME_FORMAT =
        "Invalid datetime format. Expected: {0}, Actual: {1}.";

    public const string MUST_NOT_BE_EMPTY =
        "{0} must not be empty.";
    
    public const string MUST_NOT_BE_NULL_OR_EMPTY =
        "{0} must not be null or empty.";
    
    public const string MUST_BE_GREATER_THAN_ZERO =
        "{0} must be greater than zero.";

    public const string RESP_MISMATCH =
        "RESP mismatch. Expected: {0}, Actual: {1}.";
    
    public const string RESP_ERROR =
        "RESP error: {0}.";
    
    public const string RESP_INVALID_BULK_LENGTH =
        "RESP invalid bulk length: {0}.";
    
    public const string RESP_INVALID_ARRAY_LENGTH =
        "RESP invalid array length: {0}.";

    public const string RESP_UNSUPPORTED_TYPE =
        "RESP unsupported type: {0}.";

    public const string RESP_PARSE_ERROR =
        "RESP parse error: {0}";
    
    public const string REDIS_DISCONNECTED =
        "Redis disconnected.";

    public const string OPERATION_CANCELED =
        "Operation canceled. Method: {0}.";
}