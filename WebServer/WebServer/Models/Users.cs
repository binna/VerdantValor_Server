namespace WebServer.Models
{
    public class Users
    {
        public ulong userId { get; set; }
        public string? nickname { get; set; }
        public string email { get; set; }
        public string pw { get; set; }
        public string? deviceId { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }
}
