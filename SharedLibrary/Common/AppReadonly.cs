using System.Text;

namespace WebServer;

public class AppReadonly
{
#if LIVE
    public static byte[] REQ_ENCRYPT_KEY { get; private set; }
#endif

    public static void Init(string reqEncryptKey)
    {
#if LIVE
        REQ_ENCRYPT_KEY = Encoding.UTF8.GetBytes(reqEncryptKey);
#endif
    }
}