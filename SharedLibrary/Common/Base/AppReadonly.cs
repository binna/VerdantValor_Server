using System.Text;

namespace Common.Base;

public static class AppReadonly
{
    public static byte[] REQ_ENCRYPT_KEY { get; private set; }

    public static void Init(string reqEncryptKey)
    {
        REQ_ENCRYPT_KEY = Encoding.UTF8.GetBytes(reqEncryptKey);
    }
}