using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.DownloadController;

[TestClass]
public sealed class DownloadControllerEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task GetCurrentStateAsync_should_returnCurrentState()
    {
        var device = await CreateDeviceClientAsync();

        var state = await device.DownloadController.GetCurrentStateAsync(TestContext.CancellationToken);

        Assert.IsFalse(string.IsNullOrWhiteSpace(state));
    }

    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task GetSpeedInBpsAsync_should_returnSpeed()
    {
        var device = await CreateDeviceClientAsync();

        var speed = await device.DownloadController.GetSpeedInBpsAsync(TestContext.CancellationToken);

        Assert.IsTrue(speed >= 0);
    }
}
