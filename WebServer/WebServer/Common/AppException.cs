namespace WebServer.Common
{
    public sealed class AppException
    {
        public ResponseStatus status { get; }
        public string? error { get; }

        public AppException(Exception? ex)
        {
            // TODO 에러 로그
            Console.WriteLine(ex?.StackTrace);

            status = ResponseStatus.unexpectedError;
            error = ex?.Message;
        }
    }
}