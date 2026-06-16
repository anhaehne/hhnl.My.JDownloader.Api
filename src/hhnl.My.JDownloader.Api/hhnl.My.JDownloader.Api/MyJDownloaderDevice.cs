using hhnl.My.JDownloader.Api.Endpoints;
using hhnl.My.JDownloader.Api.Models;
using hhnl.My.JDownloader.Api.Utils;

namespace hhnl.My.JDownloader.Api;

public class MyJDownloaderDevice
{
	private readonly MyJDownloaderServerClient _serverClient;
	private readonly MyJDownloaderDeviceHttpClient _deviceHttpClient;
	private readonly string? _baseUrl;

	public MyJDownloaderDevice(MyJDownloaderServerClient serverClient, MyJDownloaderDeviceHttpClient deviceClient, MyJDownloaderSession session, MyJdDeviceInfo deviceInfo, string? baseUrl)
	{
		AccountsV2 = new(this);
		Captcha = new(this);
		CaptchaForward = new(this);
		Config = new(this);
		ContentV2 = new(this);
		Device = new(this);
		Dialogs = new(this);
		DownloadController = new(this);
		DownloadEvents = new(this);
		DownloadsV2 = new(this);
		Events = new(this);
		Extensions = new(this);
		Extraction = new(this);
		Flash = new(this);
		Jd = new(this);
		LinkCollector = new(this);
		LinkCrawler = new(this);
		LinkGrabberV2 = new(this);
		Log = new(this);
		Plugins = new(this);
		Polling = new(this);
		Reconnect = new(this);
		Session = new(this);
		System = new(this);
		Toolbar = new(this);
		Ui = new(this);
		Update = new(this);

		CurrentSession = session;
		CurrentDevice = deviceInfo;
		_serverClient = serverClient;
		_deviceHttpClient = deviceClient;
		_baseUrl = baseUrl;
	}

	public MyJdDeviceInfo CurrentDevice { get; }

	public MyJDownloaderSession CurrentSession { private set; get; }

	public AccountsV2Endpoint AccountsV2 { get; }

	public CaptchaEndpoint Captcha { get; }

	public CaptchaForwardEndpoint CaptchaForward { get; }

	public ConfigEndpoint Config { get; }

	public ContentV2Endpoint ContentV2 { get; }

	public DeviceEndpoint Device { get; }

	public DialogsEndpoint Dialogs { get; }

	public DownloadControllerEndpoint DownloadController { get; }

	public DownloadEventsEndpoint DownloadEvents { get; }

	public DownloadsV2Endpoint DownloadsV2 { get; }

	public EventsEndpoint Events { get; }

	public ExtensionsEndpoint Extensions { get; }

	public ExtractionEndpoint Extraction { get; }

	public FlashEndpoint Flash { get; }

	public JdEndpoint Jd { get; }

	public LinkCollectorEndpoint LinkCollector { get; }

	public LinkCrawlerEndpoint LinkCrawler { get; }

	public LinkGrabberV2Endpoint LinkGrabberV2 { get; }

	public LogEndpoint Log { get; }

	public PluginsEndpoint Plugins { get; }

	public PollingEndpoint Polling { get; }

	public ReconnectEndpoint Reconnect { get; }

	public SessionEndpoint Session { get; }

	public SystemEndpoint System { get; }

	public ToolbarEndpoint Toolbar { get; }

	public UiEndpoint Ui { get; }

	public UpdateEndpoint Update { get; }

	public async Task<TReturn> RequestAsync<TReturn>(string url, object[]? parameters = null, CancellationToken cancellationToken = default)
	{
		var fullUrl = _baseUrl is null ? url : $"{_baseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
		var deviceState = MyJDownloaderDeviceState.Authenticated;

		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				if (deviceState == MyJDownloaderDeviceState.Authenticated)
				{
					var response = await _deviceHttpClient.RequestAsync<TReturn>(fullUrl, CurrentDevice.Id, CurrentSession, parameters, cancellationToken);
					return response;
				}
				else if (deviceState == MyJDownloaderDeviceState.TokenExpired)
				{
					// Try to renew session and repeat request
					CurrentSession = await _serverClient.RenewSessionAsync(CurrentSession, cancellationToken);
					deviceState = MyJDownloaderDeviceState.Authenticated;
				}
				else if (deviceState == MyJDownloaderDeviceState.AuthenticationFailed)
				{
					// Try to login again and repeat request
					CurrentSession = await _serverClient.LoginAsync(CurrentSession, cancellationToken);
					deviceState = MyJDownloaderDeviceState.Authenticated;
				}
				else
				{
					throw new InvalidOperationException($"Invalid device state: {deviceState}");
				}
			}
			catch (MyJDownloaderApiRequestException ex) when (ex.Error.Type == "TOKEN_INVALID")
			{
				deviceState = MyJDownloaderDeviceState.TokenExpired;
			}
			catch (MyJDownloaderApiRequestException ex) when (ex.Error.Type == "AUTH_FAILED")
			{
				if (deviceState == MyJDownloaderDeviceState.AuthenticationFailed)
				{
					// Already tried to login again, give up
					throw;
				}


				deviceState = MyJDownloaderDeviceState.AuthenticationFailed;
			}
		}

		throw new OperationCanceledException("The request was cancelled.", cancellationToken);
	}

	public Task RequestAsync(string url, object[]? parameters = null, CancellationToken cancellationToken = default)
		=> RequestAsync<object>(url, parameters, cancellationToken);

	private enum MyJDownloaderDeviceState
	{
		Authenticated,
		TokenExpired,
		AuthenticationFailed
	}
}
