using System.Text.RegularExpressions;

namespace WebServer.Helpers;

public static class ValidationHelper
{
    private const string EmailPattern = "^[a-zA-Z0-9]+$";
    private const string NicknamePattern = "^[a-zA-Z0-9ㄱ-ㅎㅏ-ㅣ가-힣]+$";

    public static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, EmailPattern);
    }
    
    public static bool IsValidNickname(string email)
    {
        return Regex.IsMatch(email, NicknamePattern);
    }
}