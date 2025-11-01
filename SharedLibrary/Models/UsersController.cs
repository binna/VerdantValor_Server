using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace SharedLibrary.Models;

public class UsersController
{
    public ulong UserId { get; private set; }
    
    [MaxLength(20)]
    public string Nickname { get; private set; } = string.Empty;
    
    [MaxLength(50)]
    public string Email { get; private set; } = string.Empty;
    
    [StringLength(128)]
    public string Pw { get; private set; } = string.Empty;
    
    [MaxLength(255)]
    public string? DeviceId { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    public DateTime UpdatedAt { get; private set; }


    public static async Task<UsersController?> FromDbDataReaderAsync(DbDataReader reader, CancellationToken token = default)
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

        return new UsersController
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