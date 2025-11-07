using System.Text.Json;
using SharedLibrary.GameData.DTO;
using SharedLibrary.Protocol.Common;

namespace SharedLibrary.Common
{
    public enum EResponseStatus
    {
        // 1000 요청 성공
        Success = 1000,
        SuccessEmptyRanking = 1001,
        
        // 그 외의 에러
        InvalidAuth = 2000,

        EmptyRequiredField = 2010,
        InvalidInput =  2011,
        NoData = 2012,
        
        EmailAlphabetNumberOnly = 2020,
        EmailAlreadyExists = 2021,
        NicknameAlreadyExists = 2022,
        InvalidEmailLength = 2023,
        InvalidNicknameLength = 2024,
        ForbiddenEmail = 2025,
        ForbiddenNickname = 2026,
        NotMatchPw  = 2027,
        NicknameAlphabetKoreanNumberOnly = 2028,
        
        // 시스템 에러
        RedisError = 9997,
        DbError = 9998,
        UnexpectedError = 9999,
    }

    public sealed class ResponseStatus
    {
        private static readonly Dictionary<EResponseStatus, 
            (bool IsSuccess, Dictionary<AppEnum.ELanguage, string> Messages)> mResponseTable = [];
        
        public bool IsSuccess { get; private init; }
        public int Code { get; private init; }
        public string? Message { get; private init; }

        private ResponseStatus() { }

        private ResponseStatus(bool isSuccess, int code, string message)
        {
            IsSuccess = isSuccess;
            Code = code;
            Message = message;
        }

        public static void Init(string path)
        {
            var jsonText = File.ReadAllText(path);

            var data = JsonSerializer.Deserialize<ResponseStatusDto>(jsonText);

            if (data == null || data.Data.Count == 0)
                throw new NullReferenceException(ExceptionMessage.EMPTY_RESPONSE_STATUS);

            foreach (var item in data.Data)
            {
                var status = (EResponseStatus)item.Code;
                var messageList = item.Message;
                
                var messageDic = new Dictionary<AppEnum.ELanguage, string>
                {
                    { AppEnum.ELanguage.Ko, messageList[0] },
                    { AppEnum.ELanguage.En, messageList[1] }
                };

                mResponseTable.Add(status, (item.IsSuccess, messageDic));
            }
            
            var values = Enum.GetValues<EResponseStatus>();
            foreach (var value in values)
            {
                if (!mResponseTable.TryGetValue(value, out var result))
                    throw new InvalidOperationException($"Not set up status - {value}({(int)value})");
            }
        }

        public static ResponseStatus FromResponseStatus(EResponseStatus status, AppEnum.ELanguage language)
        {
            var responseStatus = mResponseTable[status];
            return new ResponseStatus(responseStatus.IsSuccess, (int)status, responseStatus.Messages[language]);
        }
    }
}