using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Connection;

[TestClass]
public sealed class JDownloaderConnectionTests : JDownloaderIntegrationTestBase
{
	[TestMethod]
	[Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
	public async Task CreateDeviceClientAsync_should_connect_to_real_jdownloader_container_remotely()
	{
		// Arrange
		var device = await Container.CreateDeviceClientAsync(disableDirectConnection: true, cancellationToken: TestContext.CancellationToken);

		// Act
		var pingResult = await device.Device.PingAsync(TestContext.CancellationToken);

		// Assert
		Assert.IsTrue(pingResult);
	}

	[TestMethod]
	[Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
	public async Task CreateDeviceClientAsync_should_connect_to_real_jdownloader_container_using_direct_connection()
	{
		// Arrange
		var device = await Container.CreateDeviceClientAsync(cancellationToken: TestContext.CancellationToken);

		// Act
		var pingResult = await device.Device.PingAsync(TestContext.CancellationToken);

		// Assert
		Assert.IsTrue(pingResult);
	}

	[TestMethod]
	[Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
	public async Task CreateDeviceClientAsync_should_connect_to_real_jdownloader_container_without_dependency_injection()
	{
		// Arrange
		await Container.CreateDeviceClientAsync(cancellationToken: TestContext.CancellationToken);
		var secrets = Container.Secrets;
		var device = await MyJDownloader.CreateDeviceClientAsync(
			secrets.Email,
			secrets.Password,
			secrets.DeviceName,
			Container.DirectConnectionEndpoint,
			TestContext.CancellationToken);

		// Act
		var pingResult = await device.Device.PingAsync(TestContext.CancellationToken);

		// Assert
		Assert.IsTrue(pingResult);
	}
}
