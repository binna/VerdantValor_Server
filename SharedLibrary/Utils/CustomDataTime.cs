namespace SharedLibrary.Utils;

// 시간을 미래나 과거로 바꿀 수 있는 기능(UTC를 기준)
public readonly struct CustomDateTime
{
    private static long mUtcDiffTicks;

    private readonly DateTime mDateTime;

    public CustomDateTime(DateTime dateTime)
    {
        mDateTime = dateTime;
    }

    public static CustomDateTime Now =>
        new(DateTime.UtcNow
            .AddTicks(mUtcDiffTicks));

    public DateTime DateTime => mDateTime;

    public static void SetCustomNow(DateTime dateTime)
    {
        var diffDateTime = dateTime - DateTime.UtcNow;
        mUtcDiffTicks = diffDateTime.Ticks;
    }
}