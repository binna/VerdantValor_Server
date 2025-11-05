using SharedLibrary.Common;

namespace SharedLibrary.DTOs;

public class AuthReq
{
    public string? AuthType { get; set; }
    public string? Nickname { get; set; }
    public string? Email { get; set; }
    public string? Pw { get; set; }
    public string Language { get; set; } = $"{AppConstant.ELanguage.En}";
}