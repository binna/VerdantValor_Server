namespace WebServer.Common
{
    public sealed class ResponseStatus
    {
        // 1000 요청 성공
        public static readonly ResponseStatus success = new(true, 1000, "요청에 성공하였습니다.");
        public static readonly ResponseStatus successEmptyRanking = new(true, 1001, "요청은 성공했지만 랭킹 데이터가 없습니다.");

        // 그 외 에러
        public static readonly ResponseStatus emptyJwt = new(false, 2000, "JWT를 입력해주세요.");

        public static readonly ResponseStatus emptyEmail = new(false, 2010, "Id를 입력해주세요.");
        public static readonly ResponseStatus emptyPw = new(false, 2011, "PW를 입력해주세요.");
        public static readonly ResponseStatus emptyNickname = new(false, 2012, "닉네임을 입력해주세요.");
        public static readonly ResponseStatus emptyUser = new(false, 2013, "가입한 유저가 존재하지 않습니다.");
        public static readonly ResponseStatus notMatchPw  = new(false, 2014, "비밀번호가 일치하지 않습니다.");
        public static readonly ResponseStatus emailAlreadyExists = new(false, 2015, "이미 사용중인 아이디입니다.");
        public static readonly ResponseStatus nicknameAlreadyExists = new(false, 2016, "이미 사용중인 닉네임입니다.");
        public static readonly ResponseStatus invalEmailLength = new(false, 2017, "아이디 길이 조건을 만족하지 않습니다.");
        public static readonly ResponseStatus invalNicknameLength = new(false, 2018, "닉네임 길이 조건을 만족하지 않습니다.");
        public static readonly ResponseStatus forbiddenEmail = new(false, 2019, "금지된 아이디입니다.");
        public static readonly ResponseStatus forbiddenNickname = new(false, 2020, "금지된 닉네임입니다.");
        public static readonly ResponseStatus emailAlphabetNumericOnly = new(false, 2021, "아이디는 영어와 숫자만 사용할 수 있습니다.");

        public static readonly ResponseStatus invalidRankingType = new(false, 2100, "유효하지 않은 랭킹 타입입니다.");
        public static readonly ResponseStatus invalidRankingRange = new(false, 2101, "유효하지 않은 랭킹 순위입니다.");
        public static readonly ResponseStatus invalidAuthToken = new(false, 2102, "JWT 인증에 실패했습니다.");


        // 시스템 에러
        public static readonly ResponseStatus redisError = new(false, 9997, "레디스 에러가 발생했습니다.");
        public static readonly ResponseStatus dbError = new(false, 9998, "DB 에러가 발생했습니다.");
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