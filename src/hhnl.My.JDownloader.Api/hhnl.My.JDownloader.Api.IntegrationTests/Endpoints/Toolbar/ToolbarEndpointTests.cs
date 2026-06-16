using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Toolbar;

[TestClass]
public sealed class ToolbarEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task IsAvailableAsync_should_returnAvailability()
    {
        var device = await CreateDeviceClientAsync();

        _ = await device.Toolbar.IsAvailableAsync(TestContext.CancellationToken);
    }
}
