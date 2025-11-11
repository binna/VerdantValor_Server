using System.Security.Cryptography;
using System.Text;

namespace SharedLibrary.Helpers;

public static class HashHelper
{
    public static string ComputeSha512Hash(string plainText)
    {
        using SHA512 sha512 = SHA512.Create();
        
        var utf8Bytes = Encoding.UTF8.GetBytes(plainText);
        var hashBytes = sha512.ComputeHash(utf8Bytes);

        StringBuilder hashString = new();

        foreach (var hashByte in hashBytes)
        {
            // x2
            //   1바이트를 2자리 16진수로 표시
            //   한 자리일 경우 앞을 0으로 채움
            hashString.Append(hashByte.ToString("x2"));
        }
            
        return hashString.ToString();
    }

    public static bool VerifySha512Hash(string plainText, string hashText)
    {
        return string.Equals(
            ComputeSha512Hash(plainText), 
            hashText, 
            StringComparison.OrdinalIgnoreCase);
    }
}