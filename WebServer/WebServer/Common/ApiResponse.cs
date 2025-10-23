namespace WebServer.Common
{
    public class ApiResponse
    {
        public bool isSuccess { get; }
        public string message { get; }
        public int code { get; }

        public ApiResponse(ResponseStatus status)
        {
            this.isSuccess = status.isSuccess;
            this.message = status.message;
            this.code = status.code;
        }
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T? result { get; set; }

        public ApiResponse(ResponseStatus status)
           : base(status)
        { }

        public ApiResponse(ResponseStatus status, T result)
            : base(status)
        {
            this.result = result;
        }
    }
}
