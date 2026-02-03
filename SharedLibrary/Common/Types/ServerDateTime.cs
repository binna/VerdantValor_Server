using Common.Helpers;
using Org.BouncyCastle.Security;
using Shared.Constants;

namespace Common.Types;

// 시간을 미래나 과거로 바꿀 수 있는 기능(UTC를 기준)
public readonly struct ServerDateTime
{
    private const string FORMAT = "yyyy-MM-dd HH:mm:ss zzz";
    private static readonly TimeZoneInfo UTC_ZONE = TimeZoneInfo.FindSystemTimeZoneById("UTC");
    
    private static long mUtcDiffTicks;
    private readonly DateTime mDateTime;

    public ServerDateTime(DateTime dateTime)
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

    public static ServerDateTime Now =>
        new(DateTime.UtcNow
            .AddTicks(mUtcDiffTicks));

    public DateTime DateTime => mDateTime.AddTicks(mUtcDiffTicks);

    public static void SetServerTimeNow(ServerDateTime targetTime)
    {
        SetServerTimeNow(targetTime.DateTime);
    }
    
    public static void SetServerTimeNow(DateTime targetTime)
    {
        var diffDateTime = targetTime - DateTime.UtcNow;
        mUtcDiffTicks = diffDateTime.Ticks;
    }
    
    public static void ResetServerTimeNow()
    {
        mUtcDiffTicks = 0;
    }

    public static string ToString(ServerDateTime datetime)
    {
        return datetime.DateTime.ToString(FORMAT);
    }
    
    public static ServerDateTime ToCustomDateTime(string datetime)
    {
        if (!ValidationHelper.IsDateTimeFormat(datetime))
            throw new InvalidParameterException(
                ExceptionMessage.INVALID_DATETIME_FORMAT);

        return new ServerDateTime(
            DateTimeOffset.Parse(datetime).UtcDateTime);
    }
}