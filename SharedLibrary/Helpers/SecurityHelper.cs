using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SharedLibrary.Protocol.DTOs;
using WebServer;

namespace SharedLibrary.Helpers;

public static class SecurityHelper
{
    public static string ComputeSha512Hash(string plainText)
    {
        using var sha512 = SHA512.Create();
        
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

    public static T? DecryptPayload<T>(EncryptReq request)  
    {
        using var aesCcm = new AesCcm(AppReadonly.REQ_ENCRYPT_KEY);
        
        var nonceBytes = Convert.FromBase64String(request.Nonce);
        var dataBytes = Convert.FromBase64String(request.Data);
        var tagBytes = Convert.FromBase64String(request.Tag);
        var plainBytes = new byte[dataBytes.Length];
    
        aesCcm.Decrypt(nonceBytes, dataBytes, tagBytes, plainBytes);

        var plaintext = Encoding.UTF8.GetString(plainBytes);
    
        return JsonSerializer.Deserialize<T>(plaintext);
    }
    
    public static byte[] DecryptPayloadToBytes(EncryptReq request)  
    {
        using var aesCcm = new AesCcm(AppReadonly.REQ_ENCRYPT_KEY);
        
        var nonceBytes = Convert.FromBase64String(request.Nonce);
        var dataBytes = Convert.FromBase64String(request.Data);
        var tagBytes = Convert.FromBase64String(request.Tag);
        var plainBytes = new byte[dataBytes.Length];
    
        aesCcm.Decrypt(nonceBytes, dataBytes, tagBytes, plainBytes);

        return plainBytes;
    }
}