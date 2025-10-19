using WebServer.Contexts;

namespace WebServer.Common
{
    public sealed class ResponseStatus
    {
        // 1000 요청 성공
        public static readonly ResponseStatus success = new(true, 1000, "요청에 성공하였습니다.");

        // 그 외 에러
        public static readonly ResponseStatus emptyJwt = new(false, 2000, "JWT를 입력해주세요.");

        public static readonly ResponseStatus emptyId = new(false, 2010, "Id를 입력해주세요.");
        public static readonly ResponseStatus emptyPw = new(false, 2011, "PW를 입력해주세요.");
        public static readonly ResponseStatus emptyUser = new(false, 2012, "가입한 유저가 존재하지 않습니다.");
        public static readonly ResponseStatus notMatchPw  = new(false, 2013, "비밀번호가 일치하지 않습니다.");
        public static readonly ResponseStatus idAlreadyExists = new(false, 2014, "아이디가 이미 존재합니다.");
        public static readonly ResponseStatus invalidIdLength = new(false, 2015, $"아이디 길이 조건을 만족하지 않습니다({AppConstant.idMinLength}-{AppConstant.idMaxLength}).");
        public static readonly ResponseStatus invalidNickname = new(false, 2016, "사용할 수 없는 닉네임입니다.");
        public static readonly ResponseStatus invalidId = new(false, 2017, "사용할 수 없는 아이디입니다.");


        public static readonly ResponseStatus unexpectedError = new(false, 9999, "예상하지 못한 오류가 발생했습니다.");


        public bool isSuccess { get; }
        public int code { get; }
        public string message { get; }

        private ResponseStatus(bool isSuccess, int code, string message)
        {
            this.isSuccess = isSuccess;
            this.code = code;
            this.message = message;
        }
    }
}