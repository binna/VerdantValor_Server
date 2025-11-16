using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharedLibrary.Types;

namespace SharedLibrary.Efcore.Converter;

public class ServerDateTimeConverter : ValueConverter<ServerDateTime, DateTime>
{
    public ServerDateTimeConverter()
        : base(time => time.DateTime, time => new ServerDateTime(time))
    { }
}