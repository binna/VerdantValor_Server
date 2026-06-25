namespace Common;

public class ShareServerConst
{
    public const int SESSION_EXPIRE_MINUTES = 5;
    public const int SESSION_EXPIRE_MS = SESSION_EXPIRE_MINUTES * 60 * 1000;
    public const int LOCK_EXPIRY_MS = 1000;
    
    public const int CORE_DB_NUM = 0;
    public const int LOCK_DB_NUM = 1;
    public const int WEB_SESSION_DB_NUM = 2;
    public const int USER_SESSION_DB_NUM = 3;
}