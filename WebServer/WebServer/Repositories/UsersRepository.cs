using WebServer.Configs;

namespace WebServer.Models.Repositories
{
    public sealed class UsersRepository
    {
        private readonly ILogger<UsersRepository> logger;
        private readonly DbFactory dbConfig;

        public UsersRepository(ILogger<UsersRepository> logger, 
                               DbFactory dbConfig)
        {
            this.logger = logger;
            this.dbConfig = dbConfig;
        }
    }
}