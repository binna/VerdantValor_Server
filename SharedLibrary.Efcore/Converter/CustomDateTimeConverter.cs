using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharedLibrary.Utils;

public class CustomDateTimeConverter : ValueConverter<CustomDateTime, DateTime>
{
    public CustomDateTimeConverter()
        : base(time => time.DateTime, time => new CustomDateTime(time))
    { }
}