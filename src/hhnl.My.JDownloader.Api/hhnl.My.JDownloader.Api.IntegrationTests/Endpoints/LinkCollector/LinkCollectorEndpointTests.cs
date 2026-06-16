using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;
using hhnl.My.JDownloader.Api.Models;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.LinkCollector;

[TestClass]
public sealed class LinkCollectorEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task PackageCountAsync_should_returnPackageCount()
    {
        var device = await CreateDeviceClientAsync();

        var count = await device.LinkCollector.PackageCountAsync(TestContext.CancellationToken);

        Assert.IsTrue(count >= 0);
    }

    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task QueryLinksAsync_should_returnLinks()
    {
        var device = await CreateDeviceClientAsync();

        var links = await device.LinkCollector.QueryLinksAsync(new MyJdApiQuery { MaxResults = 1 }, TestContext.CancellationToken);

        Assert.IsNotNull(links);
    }
}
