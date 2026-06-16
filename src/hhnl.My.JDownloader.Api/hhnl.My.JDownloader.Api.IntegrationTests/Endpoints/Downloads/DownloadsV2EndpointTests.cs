using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;
using hhnl.My.JDownloader.Api.Models;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Downloads;

[TestClass]
public sealed class DownloadsV2EndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task PackageCountAsync_should_returnPackageCount()
    {
        var device = await CreateDeviceClientAsync();

        var count = await device.DownloadsV2.PackageCountAsync(TestContext.CancellationToken);

        Assert.IsTrue(count >= 0);
    }

    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task QueryPackagesAsync_should_returnPackages()
    {
        var device = await CreateDeviceClientAsync();

        var packages = await device.DownloadsV2.QueryPackagesAsync(new MyJdPackageQueryV2 { MaxResults = 1 }, TestContext.CancellationToken);

        Assert.IsNotNull(packages);
    }
}
