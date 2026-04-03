namespace WebServer.options;

public class RedisOption
{
    public bool Enabled { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public long LockExpiryMs { get; set; }
    public int CoreDbNum { get; set; }
    public int LockDbNum { get; set; }
    public int SessionDbNum { get; set; }
}