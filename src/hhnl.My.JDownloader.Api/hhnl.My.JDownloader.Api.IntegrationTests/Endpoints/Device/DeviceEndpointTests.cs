using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Device;

[TestClass]
public sealed class DeviceEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task GetDirectConnectionInfosAsync_should_returnDirectConnectionInfos()
    {
        var device = await CreateDeviceClientAsync();

        var infos = await device.Device.GetDirectConnectionInfosAsync(TestContext.CancellationToken);

        Assert.IsNotNull(infos);
    }

    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task PingAsync_should_returnTrue()
    {
        var device = await CreateDeviceClientAsync();

        var result = await device.Device.PingAsync(TestContext.CancellationToken);

        Assert.IsTrue(result);
    }
}
