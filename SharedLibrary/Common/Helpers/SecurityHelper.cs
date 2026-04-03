using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Common.Error;
using Mysqlx.Expect;
using Protocol.Web.Dtos;

namespace Common.Helpers;

public class SecurityHelper : ISecurityHelper
{
    private const int NONCE_SIZE = 12;
    private const int TAG_SIZE = 16;
    
    private readonly byte[] mEncryptKey;

    public SecurityHelper(string encryptKey)
    {
        if (string.IsNullOrWhiteSpace(encryptKey))
            throw new ArgumentNullException(
                nameof(encryptKey), 
                string.Format(ErrorMessages.MUST_NOT_BE_NULL, "Encrypt Key"));
        
        mEncryptKey = Encoding.UTF8.GetBytes(encryptKey);

        if (mEncryptKey.Length != 32
            && mEncryptKey.Length != 24
            && mEncryptKey.Length != 16)
        {
            throw new ArgumentOutOfRangeException(
                nameof(encryptKey),
                string.Format(ErrorMessages.MUST_BE_VALID_AES_KEY_SIZE, mEncryptKey.Length));
        }
    }
    
    public static bool IsValidEncryptionKey(string encryptKey)
    {
        if (string.IsNullOrWhiteSpace(encryptKey))
            return false;

        if (encryptKey.Length != 32
            && encryptKey.Length != 24
            && encryptKey.Length != 16)
            return false;

        return true;
    }

    public string ComputeSha512Hash(string plainText)
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

    public bool VerifySha512Hash(string plainText, string hashText)
    {
        return string.Equals(
            ComputeSha512Hash(plainText), 
            hashText, 
            StringComparison.OrdinalIgnoreCase);
    }

    public EncryptReq EncryptPayload<T>(T data)
    {
        if (data == null)
            throw new ArgumentNullException(
                nameof(data),
                string.Format(ErrorMessages.MUST_NOT_BE_NULL, nameof(data)));
        
        using var aesCcm = new AesCcm(mEncryptKey);

        var nonceBytes = RandomNumberGenerator.GetBytes(NONCE_SIZE);
        var tagBytes = new byte[TAG_SIZE];
        
        var dataText = JsonSerializer.Serialize(data);
        var dataBytes = Encoding.UTF8.GetBytes(dataText);
        var cipherBytes = new byte[dataBytes.Length];

        aesCcm.Encrypt(nonceBytes, dataBytes, cipherBytes, tagBytes);

        return new EncryptReq
        {
            Nonce = Convert.ToBase64String(nonceBytes),
            Tag = Convert.ToBase64String(tagBytes),
            Data = Convert.ToBase64String(cipherBytes)
        };
    }

    public T DecryptPayload<T>(EncryptReq request)
    {
        using var aesCcm = new AesCcm(mEncryptKey);
        
        var nonceBytes = Convert.FromBase64String(request.Nonce);
        var dataBytes = Convert.FromBase64String(request.Data);
        var tagBytes = Convert.FromBase64String(request.Tag);
        var plainBytes = new byte[dataBytes.Length];
    
        aesCcm.Decrypt(nonceBytes, dataBytes, tagBytes, plainBytes);

        var plaintext = Encoding.UTF8.GetString(plainBytes);
        return JsonSerializer.Deserialize<T>(plaintext);
    }
    
    public byte[] DecryptPayloadToBytes(EncryptReq request)
    {
        using var aesCcm = new AesCcm(mEncryptKey);
        
        var nonceBytes = Convert.FromBase64String(request.Nonce);
        var dataBytes = Convert.FromBase64String(request.Data);
        var tagBytes = Convert.FromBase64String(request.Tag);
        var plainBytes = new byte[dataBytes.Length];
    
        aesCcm.Decrypt(nonceBytes, dataBytes, tagBytes, plainBytes);

        return plainBytes;
    }
}