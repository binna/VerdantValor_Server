namespace WebServer.options;

public class ServerOption
{
    public string Name { get; set; }
    public string SharedLibraryPath { get; set; }
    public string BaseDir { get; } = AppContext.BaseDirectory;
}