namespace WebServer.Common
{
    public class CommonResponse<T>
    {
        public bool isSuccess { get; }
        public string message { get; }
        public int code { get; }


        public T? result { get; set; }

        public CommonResponse(CommonResponseStatus status)
        {
            this.isSuccess = status.isSuccess;
            this.message = status.message;
            this.code = status.code;
        }

        public CommonResponse(CommonResponseStatus status, T result)
        {
            this.isSuccess = status.isSuccess;
            this.message = status.message;
            this.code = status.code;
            this.result = result;
        }
    }
}
