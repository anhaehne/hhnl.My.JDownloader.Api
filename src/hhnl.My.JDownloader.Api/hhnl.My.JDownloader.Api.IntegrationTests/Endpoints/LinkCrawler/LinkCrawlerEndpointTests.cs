using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.LinkCrawler;

[TestClass]
public sealed class LinkCrawlerEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task IsCrawlingAsync_should_returnCrawlingState()
    {
        var device = await CreateDeviceClientAsync();

        _ = await device.LinkCrawler.IsCrawlingAsync(TestContext.CancellationToken);
    }
}
