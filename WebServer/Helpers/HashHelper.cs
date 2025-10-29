using System.Security.Cryptography;
using System.Text;

namespace WebServer.Helpers
{
    public static class HashHelper
    {
        public static string ComputeSHA512Hash(string plainText)
        {
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] utf8Bytes = Encoding.UTF8.GetBytes(plainText);
                byte[] hashBytes = sha512.ComputeHash(utf8Bytes);

                StringBuilder hashString = new();

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    // x2
                    //   1바이트를 2자리 16진수로 표시
                    //   한 자리일 경우 앞을 0으로 채움
                    hashString.Append(hashBytes[i].ToString("x2"));
                }

                return hashString.ToString();
            }
        }

        public static bool VerifySHA512Hash(string plainText, string hashText)
        {
            return string.Equals(
                ComputeSHA512Hash(plainText), 
                hashText, 
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
