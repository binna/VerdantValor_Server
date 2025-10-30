using System.Data.Common;

namespace SharedLibrary.Models;

public class Users
{
    public ulong UserId { get; private init; }
    public string Nickname { get; private init; }
    public string Email { get; private init; }
    public string Pw { get; private init; }
    public string? DeviceId { get; private init; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private init; }


    public static async Task<Users?> FromDbDataReaderAsync(DbDataReader reader, CancellationToken token = default)
    {
        if (!await reader.ReadAsync(token))
            return null;

        var userIdIdx = reader.GetOrdinal("userId");
        var nicknameIdx = reader.GetOrdinal("nickname");
        var emailIdx = reader.GetOrdinal("email");
        var pwIdx = reader.GetOrdinal("pw");
        var deviceIdIdx = reader.GetOrdinal("deviceId");
        var createdAtIdx = reader.GetOrdinal("createdAt");
        var updatedIdx = reader.GetOrdinal("updatedAt");

        return new Users
        {
            UserId =
                await reader.GetFieldValueAsync<ulong>(userIdIdx, token),
            Nickname =
                await reader.GetFieldValueAsync<string>(nicknameIdx, token),
            Email =
                await reader.GetFieldValueAsync<string>(emailIdx, token),
            Pw =
                await reader.GetFieldValueAsync<string>(pwIdx, token),
            DeviceId =
                await reader.IsDBNullAsync(deviceIdIdx, token) ?
                    null : await reader.GetFieldValueAsync<string>(deviceIdIdx, token),
            CreatedAt =
                await reader.GetFieldValueAsync<DateTime>(createdAtIdx, token),
            UpdatedAt =
                await reader.GetFieldValueAsync<DateTime>(updatedIdx, token)
        };
    }
}