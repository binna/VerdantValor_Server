using Org.BouncyCastle.Security;
using SharedLibrary.Common;
using SharedLibrary.Helpers;

namespace SharedLibrary.Utils;

// 시간을 미래나 과거로 바꿀 수 있는 기능(UTC를 기준)
public readonly struct CustomDateTime
{
    private const string FORMAT = "yyyy-MM-dd HH:mm:ss zzz";
    private static readonly TimeZoneInfo UTC_ZONE = TimeZoneInfo.FindSystemTimeZoneById("UTC");
    
    private static long mUtcDiffTicks;
    private readonly DateTime mDateTime;

    public CustomDateTime(DateTime dateTime)
    {
        switch (dateTime.Kind)
        {
            case DateTimeKind.Utc:
                mDateTime = dateTime;
                return; 
            case DateTimeKind.Local:
                mDateTime = dateTime.ToUniversalTime();
                return;
        }
        
        mDateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, UTC_ZONE);
    }

    public static CustomDateTime Now =>
        new(DateTime.UtcNow
            .AddTicks(mUtcDiffTicks));

    public DateTime DateTime => mDateTime.AddTicks(mUtcDiffTicks);

    public static void SetCustomNow(CustomDateTime targetTime)
    {
        SetCustomNow(targetTime.DateTime);
    }
    
    public static void SetCustomNow(DateTime targetTime)
    {
        var diffDateTime = targetTime - DateTime.UtcNow;
        mUtcDiffTicks = diffDateTime.Ticks;
    }
    
    public static void ResetCustomNow()
    {
        mUtcDiffTicks = 0;
    }

    public static string ToString(CustomDateTime datetime)
    {
        return datetime.DateTime.ToString(FORMAT);
    }
    
    public static CustomDateTime ToCustomDateTime(string datetime)
    {
        Console.WriteLine(datetime);
        if (!ValidationHelper.IsDateTimeFormat(datetime))
            throw new InvalidParameterException(
                ExceptionMessage.INVALID_DATETIME_FORMAT);

        return new CustomDateTime(
            DateTimeOffset.Parse(datetime).UtcDateTime);
    }
}