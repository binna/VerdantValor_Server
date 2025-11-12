namespace WebServer;

public class AppReadonly
{
#if LIVE
    public static readonly byte[] REQ_ENCRYPT_KEY;
#endif
    
    private ILogger<AppReadonly> mLogger;
    
    public AppReadonly(
        ILogger<AppReadonly> logger,
        IConfiguration configuration)
    {
        mLogger = logger;
        
#if LIVE
        var reqEncryptKey = configuration["ReqEncryptKey"];
        if (string.IsNullOrWhiteSpace(reqEncryptKey))
        {
            mLogger.LogCritical("Configurations are missing required fields. {@reqEncryptKey}", 
                new { reqEncryptKey });
            Environment.Exit(1);
        }
#endif
    }
}