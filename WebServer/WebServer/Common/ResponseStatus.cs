namespace WebServer.Common
{
    public sealed class CommonResponseStatus
    {
        // 1000 요청 성공
        public static readonly CommonResponseStatus success = new(true, 1000, "요청에 성공하였습니다.");

        // 그 외 에러
        public static readonly CommonResponseStatus emptyJwt = new(false, 2000, "JWT를 입력해주세요.");

        public static readonly CommonResponseStatus emptyId = new(false, 2010, "Id를 입력해주세요.");
        public static readonly CommonResponseStatus emptyPw = new(false, 2011, "PW를 입력해주세요.");
        public static readonly CommonResponseStatus emptyUser = new(false, 2012, "가입한 유저가 존재하지 않습니다.");
        public static readonly CommonResponseStatus notMatchPw  = new(false, 2013, "비밀번호가 일치하지 않습니다.");


        public static readonly CommonResponseStatus unexpectedError = new(false, 9999, "예상하지 못한 오류가 발생했습니다.");

        public bool isSuccess { get; }
        public int code { get; }
        public string message { get; }

        private CommonResponseStatus(bool isSuccess, int code, string message)
        {
            this.isSuccess = isSuccess;
            this.code = code;
            this.message = message;
        }
    }
}