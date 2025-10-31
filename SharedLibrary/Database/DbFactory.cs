// using MySql.Data.MySqlClient;
//
// namespace SharedLibrary.Database;
//
// // DbConfig
// //      DB 설정(환경 정보)을 관리
// // DbFactory
// //      DB 연결 객체를 생성해주는 팩토리
// // 해당 클래스는 Config와 팩토리 역할을 동시에 한다
// public sealed class DbFactory
// {
//     private readonly string mConnUrl;
//     
//     private readonly DbFactory _instance ;
//     
//     public DbFactory Instance { get; }
//     
//     public static void Init(string connUrl)
//     {
//         if (mConnUrl != null)
//             throw new InvalidOperationException("DbFactory already initialized.");
//
//         _connectionString = connUrl ?? throw new ArgumentNullException(nameof(connUrl));
//     }
//
//
//     // public DbFactory(
//     //     ILogger<DbFactory> logger, 
//     //     IConfiguration configuration)
//     // {
//     //     mLogger = logger;
//     //
//     //     var connUrl = configuration["DB:MySQL:Url"];
//     //
//     //     if (connUrl == null)
//     //     {
//     //         mLogger.LogCritical("DB Connection Fail");
//     //         Environment.Exit(1);
//     //     }
//     //
//     //     mConnUrl = connUrl;
//     //     mLogger.LogInformation("DB Connection Success");
//     // }
//
//     public MySqlConnection CreateConnection()
//     {
//         // 매번 새 커넥션을 쓰는 것처럼 보이지만
//         // 드라이브에서 커넥션 풀까지 코드가 짜여 있기 때문에
//         // 커넥션 풀 재사용 한다
//         return new MySqlConnection(mConnUrl);
//     }
// }