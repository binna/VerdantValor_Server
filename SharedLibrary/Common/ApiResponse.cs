namespace SharedLibrary.Common;

public class ApiResponse(ResponseStatus status)
{
    public bool IsSuccess { get; } = status.IsSuccess;
    public string Message { get; } = status.Message;
    public int Code { get; } = status.Code;
}

public class ApiResponse<T> : ApiResponse
{
    public T? Result { get; set; }

    public ApiResponse(ResponseStatus status)
       : base(status)
    { }

    public ApiResponse(ResponseStatus status, T result)
        : base(status)
    {
        this.Result = result;
    }
}
