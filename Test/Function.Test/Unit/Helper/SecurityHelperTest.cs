using Common.Helpers;
using Protocol.Web.Dtos;
using Shared.Types;
using Xunit.Abstractions;

namespace Function.Test.Unit.Helper;

public class SecurityHelperTest
{
    private readonly ITestOutputHelper mOutput;
    
    public SecurityHelperTest(ITestOutputHelper output)
    {
        mOutput = output;
    }
    
    [Fact]
    public void Test_생성자_EncryptKey이_null이면_ArgumentNullException_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new SecurityHelper(null));
    }

    // 길이 조건: 16, 24, 32
    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("ABC")]
    [InlineData("ABCDFGHIJKLMNOP")]
    [InlineData("ABCDFGHIJKLMNOPQRSTUVWXYZ")]
    [InlineData("ABCDFGHIJKLMNOPQRSTUVWXYZABCDFGHIJKLMNOPQRSTUVWXYZ")]
    public void Test_생성자_EncryptKey_길이조건이_아닐때_ArgumentOutOfRangeException_Throw(string encryptKey)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SecurityHelper(encryptKey));
    }

    [Theory]
    [InlineData("ABCDFGHIJKLMNOPQ")]
    [InlineData("ABCDFGHIJKLMNOPQRSTUVWXY")]
    [InlineData("ABCDFGHIJKLMNOPQRSTUVWXYZABCDFGH")]
    public void Test_생성됨(string encryptKey)
    {
        new SecurityHelper(encryptKey);
    }
    
    [Theory]
    [InlineData("ABCDFGHIJKLMNOPQ")]
    [InlineData("ABCDFGHIJKLMNOPQRSTUVWXY")]
    [InlineData("ABCDFGHIJKLMNOPQRSTUVWXYZABCDFGH")]
    public void Test_EncryptPayload_DecryptPayload_왕복시_원본과_동일(string encryptKey)
    {
        var securityHelper = new SecurityHelper(encryptKey);
        
        var req = new GetRankReq
        {
            Scope = $"{ERankingScope.Global}",
            Type = $"{ERanking.All}",
            Limit = 100
        };
        
        var encryptReq = securityHelper.EncryptPayload(req);
        
        Assert.NotNull(encryptReq);
        Assert.False(string.IsNullOrWhiteSpace(encryptReq.Nonce));
        Assert.False(string.IsNullOrWhiteSpace(encryptReq.Tag));
        Assert.False(string.IsNullOrWhiteSpace(encryptReq.Data));
        
        var decryptReq = securityHelper.DecryptPayload<GetRankReq>(encryptReq);
        
        Assert.Equal(req.Scope, decryptReq.Scope);
        Assert.Equal(req.Type, decryptReq.Type);
        Assert.Equal(req.Limit, decryptReq.Limit);
    }
}