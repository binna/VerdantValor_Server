namespace WebServer.Pipeline;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SkipDbContextActionFilterAttribute : Attribute
{ }