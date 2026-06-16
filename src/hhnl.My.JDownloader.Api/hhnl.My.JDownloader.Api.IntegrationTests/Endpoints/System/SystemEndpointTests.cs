using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.System;

[TestClass]
public sealed class SystemEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task GetSystemInfosAsync_should_returnSystemInfos()
    {
        var device = await CreateDeviceClientAsync();

        var infos = await device.System.GetSystemInfosAsync(TestContext.CancellationToken);

        Assert.IsNotNull(infos);
    }

    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task GetStorageInfosAsync_should_returnStorageInfos()
    {
        var device = await CreateDeviceClientAsync();

        var infos = await device.System.GetStorageInfosAsync("/config", TestContext.CancellationToken);

        Assert.IsNotNull(infos);
    }
}
