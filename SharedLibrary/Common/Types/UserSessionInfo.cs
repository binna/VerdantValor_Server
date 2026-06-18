namespace Common.Types;

public struct UserSessionInfo
{
    public string SessionId { get; set; }
    public string DeviceId { get; set; }
    public string ChatServerIp { get; set; }
    public string BattleServerIp { get; set; }
}