using System.Text.RegularExpressions;
using Common.GameData.Tables;
using Shared.Constants;
using Shared.Types;

namespace Common.Helpers;

public static partial class ValidationHelper
{
    [Flags]
    public enum EValidationFlags
    {
        None = 0,
        CheckBannedWord = 1 << 0
    }

    #region 패턴들
    private const string EMAIL_PATTERN = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    private const string NICKNAME_PATTERN = "^[a-zA-Z0-9ㄱ-ㅎㅏ-ㅣ가-힣]+$";
    private const string PASSWORD_PATTERN = @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[!@#$%^&*])[A-Za-z\d!@#$%^&*]{8,64}$";
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
    
    public static EResponseResult IsValidEmail(string email, EValidationFlags flags)
    {
        if (string.IsNullOrWhiteSpace(email))
            return EResponseResult.EmptyRequiredField;
        
        if (email.Length is < AppConstant.EAMIL_MIN_LENGTH or > AppConstant.EAMIL_MAX_LENGTH)
            return EResponseResult.InvalidEmailLength;

        if (flags.HasFlag(EValidationFlags.CheckBannedWord)
            && BannedWordTable.ContainsBannedWord(email.Split('@')[0]))
            return EResponseResult.ForbiddenEmail;
                   
        return EmailRegex().IsMatch(email)
            ? EResponseResult.Success : EResponseResult.InvalidEmailFormat;
    }
    
    public static EResponseResult IsValidNickname(string nickname, EValidationFlags flags)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            return EResponseResult.EmptyRequiredField;
                
        if (nickname.Length is < AppConstant.NICKNAME_MIN_LENGTH or > AppConstant.NICKNAME_MAX_LENGTH)
            return EResponseResult.InvalidNicknameLength;

        if (flags.HasFlag(EValidationFlags.CheckBannedWord)
            && BannedWordTable.ContainsBannedWord(nickname))
            return EResponseResult.ForbiddenNickname;

        return NicknameRegex().IsMatch(nickname)
            ? EResponseResult.Success : EResponseResult.InvalidNicknameFormat;
    }

    public static EResponseResult IsValidPassWord(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return EResponseResult.EmptyRequiredField;
        
        return NicknameRegex().IsMatch(password)
            ? EResponseResult.Success : EResponseResult.InvalidPasswordFormat;
    }

    public static bool IsDateTimeFormat(string datetime)
    {
        return DatetimeRegex().IsMatch(datetime);
    }
}