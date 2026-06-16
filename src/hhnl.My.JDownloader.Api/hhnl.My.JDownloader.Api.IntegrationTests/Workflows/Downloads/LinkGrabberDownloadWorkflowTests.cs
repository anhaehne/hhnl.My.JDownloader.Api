using hhnl.My.JDownloader.Api.Enums;
using hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;
using hhnl.My.JDownloader.Api.Models;
using System.Text;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Workflows.Downloads;

[TestClass]
public sealed class LinkGrabberDownloadWorkflowTests : JDownloaderIntegrationTestBase
{
	[TestMethod]
	[Timeout(TestTimeoutMilliseconds, CooperativeCancellation = true)]
	public async Task LinkGrabberDownloadWorkflow_should_downloadFileAndDeleteLinks()
	{
		// Tests the real JDownloader workflow from link collection to download list cleanup.
		var device = await CreateDeviceClientAsync();
		var fileName = $"jdownloader-integration-{Guid.NewGuid():N}.txt";
		var packageName = $"IntegrationTest-{Guid.NewGuid():N}";
		var fileContent = Encoding.UTF8.GetBytes($"Downloaded by {nameof(LinkGrabberDownloadWorkflowTests)} at {DateTimeOffset.UtcNow:O}.");
		await using var fileServer = await StaticFileContainer.StartAsync(Container.Network, fileName, fileContent, TestContext.CancellationToken);

		IReadOnlyCollection<long> linkIds = [];
		IReadOnlyCollection<long> packageIds = [];

		try
		{
			var job = await device.LinkGrabberV2.AddLinksAsync(new MyJdAddLinksQueryV2
			{
				AssignJobID = true,
				Autostart = false,
				DestinationFolder = "/output/integration-tests",
				Links = fileServer.ContainerUrl,
				OverwritePackagizerRules = true,
				PackageName = packageName,
			}, TestContext.CancellationToken);

			TestContext.WriteLine($"Added link grabber job {job.Id} for {fileServer.ContainerUrl}.");

			var crawledLinks = await WaitForLinkGrabberLinksAsync(device, job.Id, fileName);
			linkIds = crawledLinks.Select(x => x.UUID).ToArray();
			packageIds = crawledLinks.Select(x => x.PackageUUID).Distinct().ToArray();

			await device.LinkGrabberV2.MoveToDownloadListAsync(linkIds, packageIds, TestContext.CancellationToken);

			var downloadLinks = await WaitForDownloadLinksAsync(device, packageName, requireFinished: false);
			linkIds = downloadLinks.Select(x => x.UUID).ToArray();
			packageIds = downloadLinks.Select(x => x.PackageUUID).Distinct().ToArray();

			await device.DownloadController.StartAsync(TestContext.CancellationToken);

			var finishedLinks = await WaitForDownloadLinksAsync(device, packageName, requireFinished: true);

			Assert.IsTrue(finishedLinks.All(x => x.Finished));
			Assert.IsTrue(finishedLinks.All(x => x.BytesLoaded == fileContent.Length));

			var removedPackageIds = packageIds;
			await device.DownloadsV2.RemoveLinksAsync(linkIds, removedPackageIds, TestContext.CancellationToken);
			linkIds = [];
			packageIds = [];

			var remainingLinks = await QueryDownloadLinksAsync(device, removedPackageIds);
			Assert.AreEqual(0, remainingLinks.Count);
		}
		finally
		{
			await RemoveLinksIfPresentAsync(device, linkIds, packageIds);
		}
	}

	private async Task<IReadOnlyCollection<MyJdCrawledLinkV2>> WaitForLinkGrabberLinksAsync(MyJDownloaderDevice device, long jobId, string fileName)
		=> await WaitForAsync(async () =>
		{
			var linksByJob = await device.LinkGrabberV2.QueryLinksAsync(new MyJdCrawledLinkQueryV2
			{
				BytesTotal = true,
				JobUUIDs = [jobId],
				MaxResults = 10,
				Url = true,
			}, TestContext.CancellationToken);

			if (linksByJob.Count > 0)
				return linksByJob;

			var links = await device.LinkGrabberV2.QueryLinksAsync(new MyJdCrawledLinkQueryV2
			{
				BytesTotal = true,
				MaxResults = 100,
				Url = true,
			}, TestContext.CancellationToken);

			var matchingLinks = links
				.Where(x => x.Url?.Contains(fileName, StringComparison.OrdinalIgnoreCase) == true)
				.ToArray();

			return matchingLinks.Length > 0 ? matchingLinks : null;
		}, "The added link did not appear in the link grabber.");

	private async Task<IReadOnlyCollection<MyJdDownloadLinkV2>> WaitForDownloadLinksAsync(MyJDownloaderDevice device, string packageName, bool requireFinished)
		=> await WaitForAsync(async () =>
		{
			var packageIds = await QueryDownloadPackageIdsAsync(device, packageName);
			var links = await QueryDownloadLinksAsync(device, packageIds);

			if (links.Count == 0)
				return null;
			if (requireFinished && links.Any(x => !x.Finished))
				return null;

			return links;
		}, requireFinished ? "The download did not finish." : "The moved link did not appear in the download list.");

	private async Task<IReadOnlyCollection<long>> QueryDownloadPackageIdsAsync(MyJDownloaderDevice device, string packageName)
	{
		var packages = await device.DownloadsV2.QueryPackagesAsync(new MyJdPackageQueryV2
		{
			ChildCount = true,
			Finished = true,
			MaxResults = 100,
			SaveTo = true,
		}, TestContext.CancellationToken);

		return packages
			.Where(x => string.Equals(x.Name, packageName, StringComparison.Ordinal))
			.Select(x => x.UUID)
			.ToArray();
	}

	private async Task<IReadOnlyCollection<MyJdDownloadLinkV2>> QueryDownloadLinksAsync(MyJDownloaderDevice device, IReadOnlyCollection<long> packageIds)
	{
		if (packageIds.Count == 0)
			return [];

		return await device.DownloadsV2.QueryLinksAsync(new MyJdLinkQueryV2
		{
			BytesLoaded = true,
			BytesTotal = true,
			Finished = true,
			MaxResults = 10,
			PackageUUIDs = packageIds,
			Running = true,
			Status = true,
		}, TestContext.CancellationToken);
	}

	private async Task<T> WaitForAsync<T>(Func<Task<T?>> action, string failureMessage)
		where T : class
	{
		var deadline = DateTimeOffset.UtcNow.AddMinutes(2);

		while (!TestContext.CancellationToken.IsCancellationRequested)
		{
			var result = await action();
			if (result is not null)
				return result;

			if (DateTimeOffset.UtcNow >= deadline)
				throw new TimeoutException(failureMessage);

			await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken);
		}

		throw new OperationCanceledException(failureMessage, TestContext.CancellationToken);
	}

	private async Task RemoveLinksIfPresentAsync(MyJDownloaderDevice device, IReadOnlyCollection<long> linkIds, IReadOnlyCollection<long> packageIds)
	{
		if (linkIds.Count == 0 && packageIds.Count == 0)
			return;

		try
		{
			await device.LinkGrabberV2.RemoveLinksAsync(linkIds, packageIds, TestContext.CancellationToken);
		}
		catch (MyJDownloaderApiRequestException)
		{
		}

		try
		{
			await device.DownloadsV2.CleanupAsync(linkIds, packageIds, MyJdAction.DeleteAll, MyJdMode.RemoveLinksAndDeleteFiles, MyJdSelectionType.Selected, TestContext.CancellationToken);
		}
		catch (MyJDownloaderApiRequestException)
		{
		}
	}
}
