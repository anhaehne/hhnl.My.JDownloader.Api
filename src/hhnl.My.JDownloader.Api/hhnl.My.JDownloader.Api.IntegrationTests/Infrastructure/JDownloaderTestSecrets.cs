using Microsoft.Extensions.Configuration;

namespace hhnl.My.JDownloader.Api.IntegrationTests.Infrastructure;

public sealed class JDownloaderTestSecrets
{
	public const string ConfigurationSection = "MyJDownloaderTest";

	public string Email { get; set; } = string.Empty;

	public string Password { get; set; } = string.Empty;

	public string DeviceName { get; set; } = string.Empty;

	public bool IsConfigured =>
		!string.IsNullOrWhiteSpace(Email)
		&& !string.IsNullOrWhiteSpace(Password);

	public static JDownloaderTestSecrets Load()
	{
		var configuration = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("secrets.json", optional: true)
			.AddUserSecrets<JDownloaderTestSecrets>(optional: true)
			.Build();

		var secrets = configuration
			.GetSection(ConfigurationSection)
			.Get<JDownloaderTestSecrets>()
			?? new JDownloaderTestSecrets();

		if (string.IsNullOrWhiteSpace(secrets.DeviceName))
			secrets.DeviceName = $"IntegrationTest-{Guid.NewGuid():N}"[..30];

		if (!secrets.IsConfigured)
			Assert.Fail("JDownloader test secrets are not configured. Please provide the required secrets via user secrets or a secrets.json file.");

		return secrets;
	}
}
