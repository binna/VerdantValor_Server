using SharedLibrary.Common;

namespace SharedLibrary.DTOs;

public class LoginReq
{
    public string? Id { get; set; }
    public string? Pw { get; set; }
    public string Language { get; set; } = $"{AppConstant.ELanguage.En}";
}