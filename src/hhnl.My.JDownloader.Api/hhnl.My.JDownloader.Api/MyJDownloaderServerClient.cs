using hhnl.My.JDownloader.Api.Models;
using hhnl.My.JDownloader.Api.Models.My;
using hhnl.My.JDownloader.Api.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Web;

namespace hhnl.My.JDownloader.Api;

public class MyJDownloaderServerClient(ILogger<MyJDownloaderServerClient> logger, MyJDownloaderServerHttpClient serverClient, MyJDownloaderDeviceHttpClient deviceClient, IOptions<MyJDownloaderApiOptions> options)
{
    public async Task<MyJDownloaderSession> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Logging in to My.JDownloader");

        var initialServerKey = MyJDownloaderKey.CreateServerKey(email, password);
        var initialDeviceKey = MyJDownloaderKey.CreateDeviceKey(email, password);

        return await LoginInternalAsync(initialServerKey, initialDeviceKey, email, cancellationToken);
    }

    public async Task<MyJDownloaderSession> LoginAsync(MyJDownloaderSession session, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Logging in to My.JDownloader");

        return await LoginInternalAsync(session.InitialServerKey, session.InitialDeviceKey, session.Email, cancellationToken);
    }

    private async Task<MyJDownloaderSession> LoginInternalAsync(MyJDownloaderServerKey initialServerKey, MyJDownloaderDeviceKey initialDeviceKey, string email, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Logging in to My.JDownloader");

        var loginResponse = await serverClient.GetAsync<LoginResponse>($"/my/connect?email={HttpUtility.UrlEncode(email)}&appkey={HttpUtility.UrlEncode("PlexRequest")}", initialServerKey, cancellationToken)
            ?? throw new MyJDownloaderApiException("Received empty login response");

        return MyJDownloaderSession.Create(initialServerKey, initialDeviceKey, loginResponse, email);
    }

    public async Task<MyJDownloaderSession> RenewSessionAsync(MyJDownloaderSession session, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Renewing My.JDownloader session");

        var renewResponse = await serverClient.GetAsync<LoginResponse>($"/my/reconnect?sessiontoken={HttpUtility.UrlEncode(session.Token)}&regaintoken={HttpUtility.UrlEncode(session.RegainToken)}", session.ServerKey, cancellationToken)
            ?? throw new MyJDownloaderApiException("Received empty renew session response");

        return session.CreateRenewed(renewResponse);
    }

    public async Task<IReadOnlyCollection<MyJdDeviceInfo>> GetDevicesAsync(MyJDownloaderSession session, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving devices from My.JDownloader");

        var devices = await serverClient.GetAsync<MyJDownloaderApiListResponse<MyJdDeviceInfo>>($"/my/listdevices?sessiontoken={HttpUtility.UrlEncode(session.Token)}", session.ServerKey, cancellationToken);
        return devices?.List ?? [];
    }

    public async Task<MyJDownloaderDevice> CreateDeviceClientAsync(CancellationToken cancellationToken = default)
    {
        if (options.Value.Devices.Count == 0)
            throw new MyJDownloaderApiException("No device configured. Please configure at least one device in the MyJDownloaderApiOptions.");

        if (options.Value.Devices.Count > 1)
            throw new MyJDownloaderApiException("Multiple devices configured. Please specify a device name.");

        var configuredDevice = options.Value.Devices.Single();

        return await CreateDeviceClientAsync(configuredDevice.DeviceName, cancellationToken);
    }

    public async Task<MyJDownloaderDevice> CreateDeviceClientAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        var configuredDevice = options.Value.Devices.SingleOrDefault(d => d.DeviceName == deviceName)
            ?? throw new MyJDownloaderApiException($"Device with name '{deviceName}' not found in configuration.");

        var session = await LoginAsync(configuredDevice.Email, configuredDevice.Password, cancellationToken);
        var devices = await GetDevicesAsync(session, cancellationToken);
        var device = devices.SingleOrDefault(d => d.Name == configuredDevice.DeviceName)
            ?? throw new MyJDownloaderApiException($"Device with name '{configuredDevice.DeviceName}' not found. Make sure My.JDownloader is enabled in your JDownloader settings.");

        return await CreateDeviceClientAsync(session, device, default, configuredDevice.CustomDirectConnectionEndpoint, cancellationToken);
    }

    public async Task<MyJDownloaderDevice> CreateDeviceClientAsync(MyJDownloaderSession session, MyJdDeviceInfo device, bool? disableRemoteConnections = null, string? customDirectConnectionEndpoint = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating device client for device '{DeviceName}'", device.Name);

        if (customDirectConnectionEndpoint is not null)
        {
            logger.LogInformation("Using custom direct connection endpoint '{CustomDirectConnectionEndpoint}'", customDirectConnectionEndpoint);

            await deviceClient.RequestAsync<bool>(customDirectConnectionEndpoint + "/device/ping", device, session, null, cancellationToken);

            logger.LogInformation("Successfully connected directly to the device using custom endpoint.");

            return new MyJDownloaderDevice(this, deviceClient, session, device, customDirectConnectionEndpoint);
        }

        var connectionInfo = await deviceClient.RequestAsync<MyJdDirectConnectionInfos>("/device/getDirectConnectionInfos", device, session, null, cancellationToken);

        if (connectionInfo.Entries.Count == 0 || options.Value.DisableDirectConnections)
        {
            if (ShouldDisableRemoteConnections(disableRemoteConnections, options.Value))
                throw new MyJDownloaderApiException("The device has no direct connection info and remote connections are disabled.");


            logger.LogInformation("The device has no direct connection info. Falling back to remote connection.");

            return new MyJDownloaderDevice(this, deviceClient, session, device, null);
        }

        logger.LogInformation("Trying to connect directly to the device ...");

        logger.LogInformation(
            "JDownloader direct connection candidates: {DirectConnectionCandidates}",
            string.Join(", ", connectionInfo.Entries.Select(entry => entry.IpAddress + ":" + entry.Port)));

        var directConnectionAttempts = connectionInfo.Entries
            .Select(TryPingDeviceDirectlyAsync)
            .ToList();

        while (directConnectionAttempts.Count != 0)
        {
            var completedAttempt = await Task.WhenAny(directConnectionAttempts);
            directConnectionAttempts.Remove(completedAttempt);

            if (completedAttempt.IsCompletedSuccessfully && completedAttempt.Result is not null)
            {
                logger.LogInformation("Successfully connected directly to the device.");
                return new MyJDownloaderDevice(this, deviceClient, session, device, completedAttempt.Result);
            }
        }

        if (ShouldDisableRemoteConnections(disableRemoteConnections, options.Value))
            throw new MyJDownloaderApiException("Could not directly connect to the device and remote connections are disabled.");

        logger.LogInformation("Could not directly connect to the device. Falling back to remote connection.");

        return new MyJDownloaderDevice(this, deviceClient, session, device, null);

        async Task<string?> TryPingDeviceDirectlyAsync(IpAndPort ipAndPort)
        {
            try
            {
                var baseUrl = $"https://{ipAndPort.IpAddress.Replace('.', '-')}.mydns.jdownloader.org:{ipAndPort.Port}";
                logger.LogInformation("Trying JDownloader direct endpoint '{DirectConnectionEndpoint}'", baseUrl);
                await deviceClient.RequestAsync<bool>(baseUrl + "/device/ping", device, session, null, cancellationToken);
                return baseUrl;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Could not connect to JDownloader direct endpoint '{DirectConnectionEndpoint}'",
                    $"https://{ipAndPort.IpAddress.Replace('.', '-')}.mydns.jdownloader.org:{ipAndPort.Port}");
                return null;
            }
        }
    }

    private static bool ShouldDisableRemoteConnections(bool? disableRemoteConnections, MyJDownloaderApiOptions options)
        => disableRemoteConnections ?? options.DisableRemoteConnections;
}
