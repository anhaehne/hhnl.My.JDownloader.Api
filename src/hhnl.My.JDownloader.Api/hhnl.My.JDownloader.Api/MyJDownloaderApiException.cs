using hhnl.My.JDownloader.Api.Models;

namespace hhnl.My.JDownloader.Api;

public class MyJDownloaderApiException(string message, Exception? innerException) : Exception(message, innerException)
{
    public MyJDownloaderApiException(string message) : this(message, null)
    {
        
    }

}

public class MyJDownloaderApiRequestException(MyJDownloaderApiRequestError error) : MyJDownloaderApiException($"An error was received from the api: {error}.")
{
    public MyJDownloaderApiRequestError Error { get; } = error;
}
