using Xunit.Abstractions;

namespace WebServer.Test.Unit.Services;

[Collection("GlobalSetup ResponseStatus")]
public class StoreServiceTest
{
    private readonly ITestOutputHelper mOutput;

    public StoreServiceTest(ITestOutputHelper output)
    {
        mOutput = output;
    }
}