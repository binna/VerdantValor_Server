using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace Ado;

// DbConfig
//      DB 설정(환경 정보)을 관리
// DbFactory
//      DB 연결 객체를 생성해주는 팩토리
// 해당 클래스는 Config와 팩토리 역할을 동시에 한다
public sealed class DbFactory
{
    private readonly string mMysqlConnUrl;
    
    public DbFactory(
        IConfiguration configuration,
        ILogger<DbFactory> logger)
    {
        var mysqlConnUrl = configuration["DB:MySQL:Url"];
        
        if (string.IsNullOrWhiteSpace(mysqlConnUrl))
        {
            logger.LogCritical("Configurations are missing required fields. {@fields}", 
                new { mysqlConnUrl });
            Environment.Exit(1);
        }
        
        logger.LogInformation("DB connection success. {@context}", new { mysqlConnUrl = mMysqlConnUrl });
    }

    public MySqlConnection CreateConnection()
    {
        if (mMysqlConnUrl == null)
            throw new ArgumentNullException(nameof(mMysqlConnUrl), "MySQL connection URL cannot be null.");
        
        // 매번 새 커넥션을 쓰는 것처럼 보이지만
        // 드라이브에서 커넥션 풀까지 코드가 짜여 있기 때문에
        // 커넥션 풀 재사용 한다
        return new MySqlConnection(mMysqlConnUrl);
    }
}