using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;
using hhnl.My.JDownloader.Api.Models;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Endpoints.Accounts;

[TestClass]
public sealed class AccountsV2EndpointTests : JDownloaderIntegrationTestBase
{
    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task ListAccountsAsync_should_returnAccounts()
    {
        var device = await CreateDeviceClientAsync();

        var accounts = await device.AccountsV2.ListAccountsAsync(new MyJdAccountQuery { MaxResults = 1 }, TestContext.CancellationToken);

        Assert.IsNotNull(accounts);
    }

    [TestMethod]
    [Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
    public async Task ListBasicAuthAsync_should_returnBasicAuthEntries()
    {
        var device = await CreateDeviceClientAsync();

        var entries = await device.AccountsV2.ListBasicAuthAsync(TestContext.CancellationToken);

        Assert.IsNotNull(entries);
    }
}
