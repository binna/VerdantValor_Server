namespace SharedLibrary.DTOs;

public class GetRankReq
{
    public string? Scope { get; set; }
    public string? Type { get; set; }
    public sbyte Limit { get; set; } = 50;

}