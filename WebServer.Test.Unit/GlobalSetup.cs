using Serilog;
using SharedLibrary.Common;

namespace WebServer.Test.Unit;

public class GlobalSetupResponseStatus
{
    public GlobalSetupResponseStatus()
    {
        var baseDir = AppContext.BaseDirectory;
        var path = Path.GetFullPath(
            Path.Combine(baseDir, AppConstant.SHARED_LIBRARY_PATH, "GameData", "Data", "ResponseStatus.json"));
        ResponseStatus.Init(path);
        Log.Information("ResponseStatus setup success. {@path}", new { jsonPath = path });
    }
}

[CollectionDefinition("GlobalSetup ResponseStatus")]
public class GlobalSetupResponseStatusCollection : ICollectionFixture<GlobalSetupResponseStatus>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}