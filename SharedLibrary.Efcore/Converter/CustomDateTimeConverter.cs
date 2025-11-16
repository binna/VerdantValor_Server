using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharedLibrary.Utils;

public class CustomDateTimeConverter : ValueConverter<ServerDateTime, DateTime>
{
    public CustomDateTimeConverter()
        : base(time => time.DateTime, time => new ServerDateTime(time))
    { }
}