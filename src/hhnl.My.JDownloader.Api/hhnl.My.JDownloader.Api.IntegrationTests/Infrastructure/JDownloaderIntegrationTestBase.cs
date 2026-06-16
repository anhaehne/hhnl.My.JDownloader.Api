namespace hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

public abstract class JDownloaderIntegrationTestBase
{
	protected const int TestTimeoutMilliseconds = 480_000;

	private static readonly SemaphoreSlim _containerLock = new(1, 1);
	private static JDownloaderTestContainer? _container;

	public TestContext TestContext { get; set; } = null!;

	protected static JDownloaderTestContainer Container
		=> _container ?? throw new InvalidOperationException("The JDownloader test container was not initialized.");

	[ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
	public static async Task ClassInitialize(TestContext testContext)
	{
		await EnsureContainerAsync(testContext.CancellationToken);
	}

	internal static async Task DisposeGlobalContainerAsync()
	{
		await _containerLock.WaitAsync();

		try
		{
			if (_container is not null)
				await _container.DisposeAsync();

			_container = null;
		}
		finally
		{
			_containerLock.Release();
		}
	}

	private static async Task EnsureContainerAsync(CancellationToken cancellationToken)
	{
		if (_container is not null)
			return;

		await _containerLock.WaitAsync(cancellationToken);

		try
		{
			if (_container is not null)
				return;

			_container = await JDownloaderTestContainer.CreateAsync(cancellationToken);
			await _container.StartAsync(cancellationToken);
		}
		finally
		{
			_containerLock.Release();
		}
	}
}
