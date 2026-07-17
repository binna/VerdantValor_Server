using Protocol.Web.Dtos;

namespace Common.Helpers;

public interface ISecurityHelper
{
    string ComputeSha512Hash(string plainText);
    bool VerifySha512Hash(string plainText, string hashText);
    EncryptReq EncryptPayload<T>(T data);
    T DecryptPayload<T>(EncryptReq request);
    byte[] DecryptPayloadToBytes(EncryptReq request);
}