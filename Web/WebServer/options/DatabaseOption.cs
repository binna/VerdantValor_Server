using WebServer.types;

namespace WebServer.options;

public class DatabaseOption
{
    public EDatabaseMode Mode { get; set; }
    public string Url { get; set; }
}