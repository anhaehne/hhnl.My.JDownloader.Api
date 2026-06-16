using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Config;

[TestClass]
public sealed class ConfigEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task ListAsync_should_returnAdvancedConfigEntries()
    {
        var device = await CreateDeviceClientAsync();

        var entries = await device.Config.ListAsync("MyJDownloaderSettings", false, true, false, false, TestContext.CancellationToken);

        Assert.IsNotNull(entries);
    }
}
