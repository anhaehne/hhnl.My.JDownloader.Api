using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;
using hhnl.My.JDownloader.Api.Models;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.LinkGrabberV2;

[TestClass]
public sealed class LinkGrabberV2EndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task GetPackageCountAsync_should_returnPackageCount()
    {
        var device = await CreateDeviceClientAsync();

        var count = await device.LinkGrabberV2.GetPackageCountAsync(TestContext.CancellationToken);

        Assert.IsTrue(count >= 0);
    }

    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task QueryLinkCrawlerJobsAsync_should_returnJobs()
    {
        var device = await CreateDeviceClientAsync();

        var jobs = await device.LinkGrabberV2.QueryLinkCrawlerJobsAsync(new MyJdLinkCrawlerJobsQueryV2(), TestContext.CancellationToken);

        Assert.IsNotNull(jobs);
    }
}
