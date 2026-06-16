using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;
using hhnl.My.JDownloader.Api.Models;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Plugins;

[TestClass]
public sealed class PluginsEndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task ListAsync_should_returnPlugins()
    {
        var device = await CreateDeviceClientAsync();

        var plugins = await device.Plugins.ListAsync(new MyJdPluginsQuery { Pattern = true, Version = true }, TestContext.CancellationToken);

        Assert.IsNotNull(plugins);
    }
}
