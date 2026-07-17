namespace Tcp;

public class Config
{
    public string Country { get; set; }
    public string Name { get; set; }
    public DatabaseInfo Database { get; set; }
    public RedisInfo Redis { get; set; }

    public class DatabaseInfo
    {
        public string Mode { get; set; }
        public string Url { get; set; }
    }

    public class RedisInfo
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }
}