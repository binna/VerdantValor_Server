using MySql.Data.MySqlClient;
using SharedLibrary.Models;

namespace SharedLibrary.Ado.DAOs;

public sealed class UsersDao
{
    private readonly DbFactory mDbFactory;
    
    public UsersDao(DbFactory dbFactory)
    {
        mDbFactory = dbFactory;
    }
    
    public async Task<bool> ExistsByEmail(string email, CancellationToken token = default)
    {
        await using var conn = mDbFactory.CreateConnection();
        await conn.OpenAsync(token);

        await using var cmd = new MySqlCommand(
            "SELECT 1 FROM users WHERE email = @email LIMIT 1", 
            conn);

        cmd.Parameters.Add("@email", MySqlDbType.VarChar).Value = email;

        await using var reader = await cmd.ExecuteReaderAsync(token);

        return await reader.ReadAsync(token);
    }

    public async Task<Users?> FindByEmail(string email, CancellationToken token = default)
    {
        await using var conn = mDbFactory.CreateConnection();
        await conn.OpenAsync(token);

        await using var cmd = new MySqlCommand(
            "SELECT * FROM users WHERE email = @email",
            conn);

        cmd.Parameters.Add("@email", MySqlDbType.VarChar).Value = email;

        await using var reader = await cmd.ExecuteReaderAsync(token);

        return await Users.FromDbDataReaderAsync(reader, token);
    }

    public async Task<Users?> FindByUserId(ulong userId, CancellationToken token = default)
    {
        await using var conn = mDbFactory.CreateConnection();
        await conn.OpenAsync(token);

        await using var cmd = new MySqlCommand(
            "SELECT * FROM users WHERE userId = @userId",
            conn);

        cmd.Parameters.Add("@userId", MySqlDbType.UInt64).Value = userId;

        await using var reader = await cmd.ExecuteReaderAsync(token);

        return await Users.FromDbDataReaderAsync(reader, token);
    }

    public async Task<bool> Save(string nickname, string email, string pw)
    {
        await using var conn = mDbFactory.CreateConnection();
        await conn.OpenAsync();

        var cmd = new MySqlCommand(
            "INSERT INTO users " +
            "(nickname, email, pw, createdAt, updatedAt) " +
            "VALUES " +
            "(@nickname, @email, @pw, @createdAt, @updatedAt);",
            conn);

        var now = DateTime.Now;

        cmd.Parameters.Add("@nickname", MySqlDbType.VarChar).Value = nickname;
        cmd.Parameters.Add("@email", MySqlDbType.VarChar).Value = email;
        cmd.Parameters.Add("@pw", MySqlDbType.VarChar).Value = pw;
        cmd.Parameters.Add("@createdAt", MySqlDbType.DateTime).Value = now;
        cmd.Parameters.Add("@updatedAt", MySqlDbType.DateTime).Value = now;

        var rows = await cmd.ExecuteNonQueryAsync();

        return rows > 0;
    }
}