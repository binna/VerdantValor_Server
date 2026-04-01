using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using Common.Types;

namespace Common.Models;

public class GameUser
{
    public ulong UserId { get; private set; }
    
    [MaxLength(20)]
    public string Nickname { get; private set; } = string.Empty;
    
    [MaxLength(50)]
    public string Email { get; private set; } = string.Empty;
    
    [StringLength(128)]
    public string Pw { get; private set; } = string.Empty;
    
    public long Gold { get; private set; }
    
    public long Exp { get; private set; }
    
    [MaxLength(255)]
    public string? DeviceId { get; private set; }
    
    public ServerDateTime CreatedAt { get; private set; }
    
    public ServerDateTime UpdatedAt { get; private set; }


    private GameUser() { }

    public GameUser(string email, string nickname, string password)
    {
        var now = ServerDateTime.Now;
        Nickname = nickname;
        Email = email;
        Pw = password;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public bool GainGold(int amount)
    {
        if (amount < 0)
            return false;

        checked
        {
            Gold += amount;
            return true;
        }
    }
    
    public bool UseGold(int amount)
    {
        if (amount < 0)
            return false;

        checked
        {
            Gold -= amount;
            return true;
        }
    }
    
    public bool GainExp(int amount)
    {
        if (amount < 0)
            return false;

        checked
        {
            Gold += amount;
            return true;
        }
    }

    public static async Task<GameUser> FromDbDataReaderAsync(DbDataReader reader, CancellationToken token = default)
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

        return new GameUser
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
                new ServerDateTime(await reader.GetFieldValueAsync<DateTime>(createdAtIdx, token)),
            UpdatedAt =
                new ServerDateTime(await reader.GetFieldValueAsync<DateTime>(updatedIdx, token))
        };
    }
}