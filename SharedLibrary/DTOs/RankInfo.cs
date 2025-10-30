namespace SharedLibrary.DTOs;

public class RankInfo
{
    public ulong UserId { get; set; }
    public required string Nickname { get; set; }
    public double Score { get; set; }
}