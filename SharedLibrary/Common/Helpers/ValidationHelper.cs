using System.Text.RegularExpressions;
using Shared.Constants;
using Shared.Types;

namespace Common.Helpers;

public static partial class ValidationHelper
{
    #region 패턴들
    private const string EMAIL_PATTERN = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    private const string NICKNAME_PATTERN = @"^[a-zA-Z0-9가-힣]+$";
    private const string PASSWORD_PATTERN = @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[!@#$%^&*])[A-Za-z\d!@#$%^&*]+$";
    private const string DATETIME_FORMAT_PATTERN = @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} [+-]\d{2}:\d{2}$";
    
    [GeneratedRegex(EMAIL_PATTERN)]
    private static partial Regex EmailRegex();
    
    [GeneratedRegex(NICKNAME_PATTERN)]
    private static partial Regex NicknameRegex();
    
    [GeneratedRegex(PASSWORD_PATTERN)]
    private static partial Regex PasswordRegex();
    
    [GeneratedRegex(DATETIME_FORMAT_PATTERN)]
    private static partial Regex DatetimeRegex();
    #endregion
    
    public static EResponseResult IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return EResponseResult.EmptyRequiredField;
        
        if (email.Split('@')[0].Length is < AppConstant.EAMIL_MIN_LENGTH or > AppConstant.EAMIL_MAX_LENGTH)
            return EResponseResult.InvalidEmailLength;

        return EmailRegex().IsMatch(email)
            ? EResponseResult.Success : EResponseResult.InvalidEmailFormat;
    }
    
    public static EResponseResult IsValidNickname(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            return EResponseResult.EmptyRequiredField;
                
        if (nickname.Length is < AppConstant.NICKNAME_MIN_LENGTH or > AppConstant.NICKNAME_MAX_LENGTH)
            return EResponseResult.InvalidNicknameLength;

        return NicknameRegex().IsMatch(nickname)
            ? EResponseResult.Success : EResponseResult.InvalidNicknameFormat;
    }

    public static EResponseResult IsValidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return EResponseResult.EmptyRequiredField;

        if (password.Length is < AppConstant.PASSWORD_MIN_LENGTH or > AppConstant.PASSWORD_MAX_LENGTH)
            return EResponseResult.InvalidPasswordLength;
        
        return PasswordRegex().IsMatch(password)
            ? EResponseResult.Success : EResponseResult.InvalidPasswordFormat;
    }

    public static bool IsDateTimeFormat(string datetime)
    {
        return DatetimeRegex().IsMatch(datetime);
    }
}