# hhnl.My.JDownloader.Api

A .NET client for the [MyJDownloader](https://my.jdownloader.org/) API.

The library lets .NET applications connect to a JDownloader device registered with a MyJDownloader account and call device endpoints such as downloads, link grabber, accounts, system information, dialogs, plugins, and update checks.

## Packages

- `hhnl.My.JDownloader.Api`: core API client.
- `hhnl.My.JDownloader.Api.DependencyInjection`: optional Microsoft dependency injection integration.

## Installation

Install the core package:

```powershell
dotnet add package hhnl.My.JDownloader.Api
```

For Microsoft dependency injection integration, install the DI package as well:

```powershell
dotnet add package hhnl.My.JDownloader.Api.DependencyInjection
```

## Requirements

- A running JDownloader instance with MyJDownloader enabled.
- A MyJDownloader account email and password.
- The JDownloader device name as it appears in MyJDownloader.
- .NET 10 or later.

## Quick Start Without Dependency Injection

Use the static helper when you just want a device client without setting up a service container.

```csharp
using hhnl.My.JDownloader.Api;

var device = await MyJDownloader.CreateDeviceClientAsync(
    email: "you@example.com",
    password: "my-jdownloader-password",
    deviceName: "My JDownloader");

var isOnline = await device.Device.PingAsync();
var packages = await device.DownloadsV2.QueryPackagesAsync(new()
{
    MaxResults = 50,
    ChildCount = true,
    Finished = true,
});
```

You can also configure client behavior:

```csharp
var device = await MyJDownloader.CreateDeviceClientAsync(
    email: "you@example.com",
    password: "my-jdownloader-password",
    deviceName: "My JDownloader",
    configureOptions: options =>
    {
        options.DisableRemoteConnections = true;
        options.DisableDirectConnections = false;
    });
```

If you already know the direct connection endpoint, pass it explicitly:

```csharp
var device = await MyJDownloader.CreateDeviceClientAsync(
    email: "you@example.com",
    password: "my-jdownloader-password",
    deviceName: "My JDownloader",
    customDirectConnectionEndpoint: "http://127.0.0.1:3129");
```

## Dependency Injection

For applications that use `IServiceCollection`, install the dependency injection package and register the client:

```csharp
using hhnl.My.JDownloader.Api;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services
    .AddMyJDownloaderApi(options =>
    {
        options.DisableRemoteConnections = false;
    })
    .AddMyJDownloaderDevice(
        email: "you@example.com",
        password: "my-jdownloader-password",
        name: "My JDownloader");

var serviceProvider = services.BuildServiceProvider();
var serverClient = serviceProvider.GetRequiredService<MyJDownloaderServerClient>();
var device = await serverClient.CreateDeviceClientAsync();
```

## Calling Endpoints

After creating a `MyJDownloaderDevice`, endpoints are exposed as properties:

```csharp
var ping = await device.Device.PingAsync();
var speed = await device.DownloadController.GetSpeedInBpsAsync();
var linkGrabberCount = await device.LinkGrabberV2.GetPackageCountAsync();
var logs = await device.Log.GetAvailableLogsAsync();
```

Most endpoints mirror the MyJDownloader API naming and return generated model types from `hhnl.My.JDownloader.Api.Models`.

## Add and Start Downloads

```csharp
using hhnl.My.JDownloader.Api.Models;

var job = await device.LinkGrabberV2.AddLinksAsync(new MyJdAddLinksQueryV2
{
    Links = "https://example.com/file.zip",
    PackageName = "Example download",
    Autostart = false,
});

var links = await device.LinkGrabberV2.QueryLinksAsync(new MyJdCrawledLinkQueryV2
{
    JobUUIDs = [job.Id],
    MaxResults = 10,
    Url = true,
});

var linkIds = links.Select(x => x.UUID).ToArray();
var packageIds = links.Select(x => x.PackageUUID).Distinct().ToArray();

await device.LinkGrabberV2.MoveToDownloadListAsync(linkIds, packageIds);
await device.DownloadController.StartAsync();
```

## Direct and Remote Connections

The client first logs in through `https://api.jdownloader.org`. When creating a device client, it can use direct connection information reported by JDownloader. If direct connection is unavailable and remote connections are allowed, it falls back to the MyJDownloader remote API.

Relevant options:

```csharp
options.DisableDirectConnections = false;
options.DisableRemoteConnections = false;
options.HttpsCertificateMode = HttpsCertificateMode.Verify;
```

Use `DisableRemoteConnections = true` when you want failures instead of falling back to the remote API.
