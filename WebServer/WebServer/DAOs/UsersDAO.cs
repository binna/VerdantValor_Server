using MySql.Data.MySqlClient;
using System.Data;
using WebServer.Configs;

namespace WebServer.Models.Repositories
{
    public sealed class UsersDAO
    {
        private readonly ILogger<UsersDAO> logger;
        private readonly DbFactory dbFactory;

        public UsersDAO(ILogger<UsersDAO> logger, 
                        DbFactory dbFactory)
        {
            this.logger = logger;
            this.dbFactory = dbFactory;
        }

        public bool existsByEmail(string email)
        {
            using var conn = dbFactory.CreateConnection();
            conn.Open();

            var cmd = new MySqlCommand(
                "SELECT 1 FROM users WHERE email = @email LIMIT 1", 
                conn);

            cmd.Parameters.Add("@email", MySqlDbType.VarChar).Value = email;

            using var reader = cmd.ExecuteReader();
            
            return reader.Read();
        }

        public Users? FindByEmail(string email)
        {
            using var conn = dbFactory.CreateConnection();
            conn.Open();

            var cmd = new MySqlCommand(
                "SELECT * FROM users WHERE email = @email",
                conn);

            cmd.Parameters.Add("@email", MySqlDbType.VarChar).Value = email;

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Users
            {
                userId = reader.GetUInt64("userId"),
                nickname = reader.GetString("nickname"),
                email = reader.GetString("email"),
                pw = reader.GetString("pw"),
                deviceId = reader.IsDBNull("deviceId") ? null : reader.GetString("deviceId"),
                createdAt = reader.GetDateTime("createdAt"),
                updatedAt = reader.GetDateTime("updatedAt")
            };
        }

        public Users? FindByUserId(long userId)
        {
            using var conn = dbFactory.CreateConnection();
            conn.Open();

            var cmd = new MySqlCommand(
                "SELECT * FROM users WHERE userId = @userId",
                conn);

            cmd.Parameters.Add("@userId", MySqlDbType.VarChar).Value = userId;

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Users
            {
                userId = reader.GetUInt64("userId"),
                nickname = reader.GetString("nickname"),
                email = reader.GetString("email"),
                pw = reader.GetString("pw"),
                deviceId = reader.IsDBNull("deviceId") ? null : reader.GetString("deviceId"),
                createdAt = reader.GetDateTime("createdAt"),
                updatedAt = reader.GetDateTime("updatedAt")
            };
        }

        public bool Save(string nickname, string email, string pw)
        {
            using var conn = dbFactory.CreateConnection();
            conn.Open();

            var cmd = new MySqlCommand(
                "INSERT INTO users " +
                "(nickname, email, pw, createdAt, updatedAt) " +
                "VALUES " +
                "(@nickname, @email, @pw, @createdAt, @updatedAt);",
                conn);

            DateTime now = DateTime.Now;

            cmd.Parameters.Add("@nickname", MySqlDbType.VarChar).Value = nickname;
            cmd.Parameters.Add("@email", MySqlDbType.VarChar).Value = email;
            cmd.Parameters.Add("@pw", MySqlDbType.VarChar).Value = pw;
            cmd.Parameters.Add("@createdAt", MySqlDbType.DateTime).Value = now;
            cmd.Parameters.Add("@updatedAt", MySqlDbType.DateTime).Value = now;

            int rows = cmd.ExecuteNonQuery();

            return rows > 0;
        }
    }
}