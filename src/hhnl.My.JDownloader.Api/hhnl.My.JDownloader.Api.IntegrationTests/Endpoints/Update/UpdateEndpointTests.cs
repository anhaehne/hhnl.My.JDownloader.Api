using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Update;

[TestClass]
public sealed class UpdateEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task IsUpdateAvailableAsync_should_returnUpdateAvailability()
    {
        var device = await CreateDeviceClientAsync();

        _ = await device.Update.IsUpdateAvailableAsync(TestContext.CancellationToken);
    }
}
