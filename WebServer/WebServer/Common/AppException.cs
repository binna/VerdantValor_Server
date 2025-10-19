using WebServer.Common;

namespace WebServer.Configs
{
    public sealed class AppException
    {
        public CommonResponseStatus status { get; }
        public string? error { get; }

        public AppException(Exception? ex)
        {
            // TODO 에러 로그
            Console.WriteLine(ex?.StackTrace);

            this.status = CommonResponseStatus.unexpectedError;
            this.error = ex?.Message;
        }
    }
}