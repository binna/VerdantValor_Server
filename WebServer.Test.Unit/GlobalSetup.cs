using Serilog;
using SharedLibrary.Common;
using SharedLibrary.GameData;

namespace WebServer.Test.Unit;

public class GlobalSetupResponseStatus
{
    public GlobalSetupResponseStatus()
    {
        var baseDir = AppContext.BaseDirectory;
        var path = Path.GetFullPath(
            Path.Combine(baseDir, AppConstant.SHARED_LIBRARY_PATH, "GameData", "Data", "ResponseStatus.json"));
        ResponseStatusTable.Init(path);
        Log.Information("Response Status setup success. {@path}", new { jsonPath = path });
        
#if LIVE
        var reqEncryptKey = "ABCDEFGHIJKLMNOP";
        AppReadonly.Init(reqEncryptKey);
        Log.Information("Request Encrypt Key setup success. {@reqEncryptKey}", new { reqEncryptKey });
#endif
    }
}

[CollectionDefinition("GlobalSetup ResponseStatus")]
public class GlobalSetupResponseStatusCollection : ICollectionFixture<GlobalSetupResponseStatus>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}