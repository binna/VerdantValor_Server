using System.Reflection;

namespace WebServer.options;

public class PathOption
{
    public string SolutionDir { get; } = Assembly.GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == "SolutionDir")?.Value; 
    
    public string SharedLibrary { get; set; }
    
    public string GameData => Path.GetFullPath(Path.Combine(SolutionDir, SharedLibrary));
}
