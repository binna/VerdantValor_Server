using System.Data.Common;

namespace WebServer.Models
{
    public class Users
    {
        public ulong userId { get; private set; }
        public string nickname { get; private set; }
        public string email { get; private set; }
        public string pw { get; private set; }
        public string? deviceId { get; private set; }
        public DateTime createdAt { get; private set; }
        public DateTime updatedAt { get; private set; }


        public static async Task<Users?> FromDbDataReaderAsync(DbDataReader reader, CancellationToken token = default)
        {
            if (!await reader.ReadAsync(token))
                return null;

            int userIdIdx = reader.GetOrdinal("userId");
            int nicknameIdx = reader.GetOrdinal("nickname");
            int emailIdx = reader.GetOrdinal("email");
            int pwIdx = reader.GetOrdinal("pw");
            int deviceIdIdx = reader.GetOrdinal("deviceId");
            int createdAtIdx = reader.GetOrdinal("createdAt");
            int updatedIdx = reader.GetOrdinal("updatedAt");

            return new Users
            {
                userId =
                    await reader.GetFieldValueAsync<ulong>(userIdIdx, token),
                nickname =
                    await reader.GetFieldValueAsync<string>(nicknameIdx, token),
                email =
                    await reader.GetFieldValueAsync<string>(emailIdx, token),
                pw =
                    await reader.GetFieldValueAsync<string>(pwIdx, token),
                deviceId =
                    await reader.IsDBNullAsync(deviceIdIdx, token) ?
                        null : await reader.GetFieldValueAsync<string>(deviceIdIdx, token),
                createdAt =
                    await reader.GetFieldValueAsync<DateTime>(createdAtIdx, token),
                updatedAt =
                    await reader.GetFieldValueAsync<DateTime>(updatedIdx, token)
            };
        }
    }
}
