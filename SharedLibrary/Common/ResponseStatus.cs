namespace SharedLibrary.Common
{
    public enum EResponseStatus
    {
        // 1000 요청 성공
        Success = 1000,
        SuccessEmptyRanking = 1001,
        
        // 그 외의 에러
        InvalidAuth = 2000,
        EmptyAuth = 2001,

        EmptyEmail = 2010,
        EmptyPw = 2011,
        EmptyNickname = 2012,
        EmptyUser = 2013,
        NotMatchPw  = 2014,
        EmailAlreadyExists = 2015,
        NicknameAlreadyExists = 2016,
        InvalidEmailLength = 2017,
        InvalidNicknameLength = 2018,
        ForbiddenEmail = 2019,
        ForbiddenNickname = 2020,
        EmailAlphabetNumericOnly = 2021,
        
        InvalidRankingType =  2100,
        InvalidRankingRange = 2101,

        // 시스템 에러
        RedisError = 9997,
        DbError = 9998,
        UnexpectedError = 9999,
    }

    public sealed class ResponseStatus
    {
        private static readonly Dictionary<EResponseStatus, 
            (bool IsSuccess, Dictionary<AppConstant.ELanguage, string> Messages)> mResponseTable = [];
        
        private static readonly Lazy<ResponseStatus> mInstance = new(() => new ResponseStatus());
        public static ResponseStatus Instance => mInstance.Value;
        
        public bool IsSuccess { get; private init; }
        public int Code { get; private init; }
        public string Message { get; private init; }

        private ResponseStatus() { }

        private ResponseStatus(bool isSuccess, int code, string message)
        {
            IsSuccess = isSuccess;
            Code = code;
            Message = message;
        }

        public void Init()
        {
            // TODO Message 같은 경우는 지금은 하드코딩 되어 있지만, 같이 공유할 수 있도록 나중에 .json과 같은 파일로 확장해보기
            
            #region 데이터 세팅
            mResponseTable.Add(EResponseStatus.Success, (true, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "요청에 성공하였습니다." },
                { AppConstant.ELanguage.En, "Request succeeded." }
            }));
            mResponseTable.Add(EResponseStatus.SuccessEmptyRanking, (true, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "요청은 성공했지만 랭킹 데이터가 없습니다." },
                { AppConstant.ELanguage.En, "Request succeeded, but ranking data is empty." }
            }));

            mResponseTable.Add(EResponseStatus.InvalidAuth, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "현재 로그인 정보를 확인할 수 없습니다.\n다시 로그인해 주세요." },
                { AppConstant.ELanguage.En, "Your login information could not be verified.\nPlease log in again." }
            }));
            mResponseTable.Add(EResponseStatus.EmptyAuth, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "요청 컨텍스트를 찾을 수 없습니다.\n세션이 손상되었을 수 있습니다." },
                { AppConstant.ELanguage.En, "Request context not found.\nThe session may be corrupted." }
            }));

            mResponseTable.Add(EResponseStatus.EmptyEmail, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "ID를 입력해주세요." },
                { AppConstant.ELanguage.En, "Please enter your ID." }
            }));
            mResponseTable.Add(EResponseStatus.EmptyPw, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "PW를 입력해주세요." },
                { AppConstant.ELanguage.En, "Please enter your password." }
            }));
            mResponseTable.Add(EResponseStatus.EmptyNickname, (false, new Dictionary<AppConstant.ELanguage, string> 
            { 
                { AppConstant.ELanguage.Ko, "닉네임을 입력해주세요." }, 
                { AppConstant.ELanguage.En, "Please enter your nickname." } 
            }));
            mResponseTable.Add(EResponseStatus.EmptyUser, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "가입한 유저가 존재하지 않습니다." },
                { AppConstant.ELanguage.En, "The registered user does not exist." }
            }));
            mResponseTable.Add(EResponseStatus.NotMatchPw, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko,"비밀번호가 일치하지 않습니다."}, 
                { AppConstant.ELanguage.En, "The password does not match." }
            }));
            mResponseTable.Add(EResponseStatus.EmailAlreadyExists, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "이미 사용 중인 아이디입니다." },
                { AppConstant.ELanguage.En, "This ID is already in use." }
            }));
            mResponseTable.Add(EResponseStatus.NicknameAlreadyExists, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "이미 사용 중인 닉네임입니다." },
                { AppConstant.ELanguage.En, "This nickname is already in use." }
            }));
            mResponseTable.Add(EResponseStatus.InvalidEmailLength, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "아이디 길이 조건을 만족하지 않습니다." },
                { AppConstant.ELanguage.En, "The ID length does not meet the requirements." }
            }));
            mResponseTable.Add(EResponseStatus.InvalidNicknameLength, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "닉네임 길이 조건을 만족하지 않습니다." },
                { AppConstant.ELanguage.En, "The nickname length does not meet the requirements." }
            }));
            mResponseTable.Add(EResponseStatus.ForbiddenEmail, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "금지된 아이디입니다." },
                { AppConstant.ELanguage.En, "This ID is not allowed." }
            }));
            mResponseTable.Add(EResponseStatus.ForbiddenNickname, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "금지된 닉네임입니다." },
                { AppConstant.ELanguage.En, "This nickname is not allowed." }
            }));
            mResponseTable.Add(EResponseStatus.EmailAlphabetNumericOnly, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "아이디는 영어와 숫자만 사용할 수 있습니다." },
                { AppConstant.ELanguage.En, "The ID can only contain English letters and numbers." }
            }));

            mResponseTable.Add(EResponseStatus.InvalidRankingType, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "유효하지 않은 랭킹 타입입니다." },
                { AppConstant.ELanguage.En, "Invalid ranking type." }
            }));
            mResponseTable.Add(EResponseStatus.InvalidRankingRange, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "유효하지 않은 랭킹 순위입니다." },
                { AppConstant.ELanguage.En, "Invalid ranking range." }
            }));

            mResponseTable.Add(EResponseStatus.RedisError, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "Redis 에러가 발생했습니다." },
                { AppConstant.ELanguage.En, "Redis error has occurred." }
            }));
            mResponseTable.Add(EResponseStatus.DbError, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "DB 에러가 발생했습니다." },
                { AppConstant.ELanguage.En, "DB error has occurred." }
            }));
            mResponseTable.Add(EResponseStatus.UnexpectedError, (false, new Dictionary<AppConstant.ELanguage, string>
            {
                { AppConstant.ELanguage.Ko, "예상하지 못한 오류가 발생했습니다." },
                { AppConstant.ELanguage.En, "An unexpected error has occurred." }
            }));
            #endregion
            
            var values = Enum.GetValues<EResponseStatus>();
            foreach (var value in values)
            {
                if (!mResponseTable.TryGetValue(value, out var result))
                    throw new InvalidOperationException($"Not set up status - {value}({(int)value})");
            }
        }

        public static ResponseStatus FromResponseStatus(EResponseStatus status, AppConstant.ELanguage language)
        {
            var responseState = mResponseTable[status];
            return new ResponseStatus(responseState.IsSuccess, (int)status, responseState.Messages[language]);
        }
    }
}