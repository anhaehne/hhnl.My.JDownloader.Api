using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using hhnl.My.JDownloader.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

public sealed class JDownloaderTestContainer : IAsyncDisposable
{
	private const string CustomizedImageName = "hhnl-my-jdownloader-api-integration-tests:latest";
	private const ushort WebPort = 5800;
	private const ushort DirectConnectionPort = 3129;

	private static readonly SemaphoreSlim _directConnectionImageLock = new(1, 1);
	private static IFutureDockerImage? _containerImage;

	private readonly IContainer _container;
	private readonly JDownloaderTestSecrets _secrets;
	private IServiceProvider? _serviceProviderNoDirectConnection;
	private IServiceProvider? _serviceProviderDirectConnection;

	private JDownloaderTestContainer(IContainer container, JDownloaderTestSecrets secrets)
	{
		_container = container;
		_secrets = secrets;
	}

	public async Task<MyJDownloaderDevice> CreateDeviceClientAsync(bool disableDirectConnection = false, CancellationToken cancellationToken = default)
	{
		if (_container.State != TestcontainersStates.Running)
			throw new InvalidOperationException("The container must be running to create a device client.");

		var serviceProvider = disableDirectConnection switch
		{
			true => _serviceProviderNoDirectConnection ??= CreateServiceProvider(_secrets, DirectConnectionEndpoint, true),
			false => _serviceProviderDirectConnection ??= CreateServiceProvider(_secrets, DirectConnectionEndpoint, false),
		};

		var serverClient = serviceProvider.GetRequiredService<MyJDownloaderServerClient>();

		var session = await serverClient.LoginAsync(_secrets.Email, _secrets.Password, cancellationToken);
		var deviceInfo = await WaitForDeviceAsync(serverClient, session, _secrets.DeviceName, cancellationToken);
		var device = await serverClient.CreateDeviceClientAsync(session, deviceInfo, disableRemoteConnections: true, DirectConnectionEndpoint, cancellationToken);

		return device;
	}

	public string DirectConnectionEndpoint => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(DirectConnectionPort)}";

	public static async Task<JDownloaderTestContainer> CreateAsync(CancellationToken cancellationToken = default)
	{
		var secrets = JDownloaderTestSecrets.Load();

		var image = await GetCustomizedImageAsync(cancellationToken);

		var waitStrategy = Wait.ForUnixContainer()
			.UntilHttpRequestIsSucceeded(request => request.ForPort(WebPort))
			.UntilExternalTcpPortIsAvailable(DirectConnectionPort);

		var builder = new ContainerBuilder(image.FullName)
			.WithAutoRemove(false)
			.WithPortBinding(WebPort, true)
			.WithEnvironment("MYJDOWNLOADER_EMAIL", secrets.Email)
			.WithEnvironment("MYJDOWNLOADER_PASSWORD", secrets.Password)
			.WithEnvironment("MYJDOWNLOADER_DEVICE_NAME", secrets.DeviceName)
			.WithEnvironment("WEB_AUDIO", "0")
			.WithEnvironment("WEB_FILE_MANAGER", "0")
			.WithEnvironment("WEB_NOTIFICATION", "0")
			.WithEnvironment("WEB_TERMINAL", "0")
			.WithWaitStrategy(waitStrategy)
			.WithPortBinding(DirectConnectionPort, true);

		return new JDownloaderTestContainer(builder.Build(), secrets);
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await _container.StartAsync(cancellationToken);
	}

	public async ValueTask DisposeAsync()
	{
		if (_serviceProviderNoDirectConnection is IAsyncDisposable asyncServiceProviderNoDirectConnection)
			await asyncServiceProviderNoDirectConnection.DisposeAsync();
		else if (_serviceProviderNoDirectConnection is IDisposable serviceProviderNoDirectConnection)
			serviceProviderNoDirectConnection.Dispose();

		if (_serviceProviderDirectConnection is IAsyncDisposable asyncServiceProviderDirectConnection)
			await asyncServiceProviderDirectConnection.DisposeAsync();
		else if (_serviceProviderDirectConnection is IDisposable serviceProviderDirectConnection)
			serviceProviderDirectConnection.Dispose();

		await _container.DisposeAsync();
	}

	private static async Task<IFutureDockerImage> GetCustomizedImageAsync(CancellationToken cancellationToken)
	{
		await _directConnectionImageLock.WaitAsync(cancellationToken);

		try
		{
			if (_containerImage is not null)
				return _containerImage;

			var dockerfileDirectory = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "TestImage");

			_containerImage = new ImageFromDockerfileBuilder()
				.WithName(CustomizedImageName)
				.WithDockerfileDirectory(dockerfileDirectory)
				.WithDockerfile("Dockerfile")
				.WithImageBuildPolicy(PullPolicy.Missing)
				.WithDeleteIfExists(false)
				.Build();

			await _containerImage.CreateAsync(cancellationToken);

			return _containerImage;
		}
		finally
		{
			_directConnectionImageLock.Release();
		}
	}

	private static ServiceProvider CreateServiceProvider(JDownloaderTestSecrets secrets, string directConnectionEndpoint, bool disableDirectConnection)
	{
		var services = new ServiceCollection();

		services.AddLogging();
		services.AddMyJDownloaderApi(options =>
		{
			options.DisableDirectConnections = disableDirectConnection;
		})
		.AddMyJDownloaderDevice(secrets.Email, secrets.Password, secrets.DeviceName, directConnectionEndpoint);

		return services.BuildServiceProvider();
	}

	private static async Task<MyJdDeviceInfo> WaitForDeviceAsync(MyJDownloaderServerClient serverClient, Utils.MyJDownloaderSession session, string deviceName, CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			var devices = await serverClient.GetDevicesAsync(session, cancellationToken);
			var device = devices.SingleOrDefault(d => d.Name == deviceName);

			if (device is not null)
				return device;

			await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
		}

		throw new OperationCanceledException($"The JDownloader device '{deviceName}' did not appear in MyJDownloader before the test timed out.", cancellationToken);
	}
}
