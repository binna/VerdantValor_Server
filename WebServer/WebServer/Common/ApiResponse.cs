namespace WebServer.Common
{
    public class ApiResponse<T>
    {
        public bool isSuccess { get; }
        public string message { get; }
        public int code { get; }


        public T? result { get; set; }

        public ApiResponse(ResponseStatus status)
        {
            this.isSuccess = status.isSuccess;
            this.message = status.message;
            this.code = status.code;
        }

        public ApiResponse(ResponseStatus status, T result)
        {
            this.isSuccess = status.isSuccess;
            this.message = status.message;
            this.code = status.code;
            this.result = result;
        }
    }
}
