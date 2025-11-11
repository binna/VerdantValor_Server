using System.Text.RegularExpressions;

namespace SharedLibrary.Helpers;

public static class ValidationHelper
{
    private const string EmailPattern = "^[a-zA-Z0-9]+$";
    private const string NicknamePattern = "^[a-zA-Z0-9ㄱ-ㅎㅏ-ㅣ가-힣]+$";
    private const string DateTimeFormat = @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} [+-]\d{2}:\d{2}$";

    public static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, EmailPattern);
    }
    
    public static bool IsValidNickname(string nickname)
    {
        return Regex.IsMatch(nickname, NicknamePattern);
    }
    
    public static bool IsDateTimeFormat(string datetime)
    {
        return Regex.IsMatch(datetime, DateTimeFormat);
    }
}