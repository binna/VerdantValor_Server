using System.Text.RegularExpressions;

namespace Common.Helpers;

public static class ValidationHelper
{
    private const string EMAIL_PATTERN = "^[a-zA-Z0-9]+$";
    private const string NICKNAME_PATTERN = "^[a-zA-Z0-9ㄱ-ㅎㅏ-ㅣ가-힣]+$";
    private const string DATETIME_FORMAT = @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} [+-]\d{2}:\d{2}$";

    public static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, EMAIL_PATTERN);
    }
    
    public static bool IsValidNickname(string nickname)
    {
        return Regex.IsMatch(nickname, NICKNAME_PATTERN);
    }
    
    public static bool IsDateTimeFormat(string datetime)
    {
        return Regex.IsMatch(datetime, DATETIME_FORMAT);
    }
}