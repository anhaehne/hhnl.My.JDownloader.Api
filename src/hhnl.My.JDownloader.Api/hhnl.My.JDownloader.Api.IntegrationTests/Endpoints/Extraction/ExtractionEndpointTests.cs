using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Extraction;

[TestClass]
public sealed class ExtractionEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task GetQueueAsync_should_returnExtractionQueue()
    {
        var device = await CreateDeviceClientAsync();

        var queue = await device.Extraction.GetQueueAsync(TestContext.CancellationToken);

        Assert.IsNotNull(queue);
    }
}
