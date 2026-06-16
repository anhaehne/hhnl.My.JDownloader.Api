using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Jd;

[TestClass]
public sealed class JdEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task UptimeAsync_should_returnUptime()
    {
        var device = await CreateDeviceClientAsync();

        var uptime = await device.Jd.UptimeAsync(TestContext.CancellationToken);

        Assert.IsTrue(uptime >= 0);
    }
}
