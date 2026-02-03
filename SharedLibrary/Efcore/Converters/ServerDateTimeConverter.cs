using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Common.Types;

namespace Efcore.Converters;

public class ServerDateTimeConverter : ValueConverter<ServerDateTime, DateTime>
{
    public ServerDateTimeConverter()
        : base(time => time.DateTime, time => new ServerDateTime(time))
    { }
}