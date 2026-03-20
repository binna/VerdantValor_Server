using Protocol.Web.Dtos;

namespace Common.Helpers;

public interface ISecurityHelper
{
    public string ComputeSha512Hash(string plainText);
    public bool VerifySha512Hash(string plainText, string hashText);
    public EncryptReq EncryptPayload<T>(T data);
    public T DecryptPayload<T>(EncryptReq request);
    public byte[] DecryptPayloadToBytes(EncryptReq request);
}