namespace WebIngest.Core.Scraping.WebClients;

public interface IWebIngestWebClient
{
    public string DownloadString(string url);
}