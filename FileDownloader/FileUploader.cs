using System.Net.Http.Headers;

namespace FileDownloader;

public class FileUploader
{
    public static async Task UploadFile(string uri, string localFilePath, string remoteFileName = "file.bin")
    {
        using var httpClient = new HttpClient();
        await using var fileStream = File.OpenRead(localFilePath);
        using var multipartFormContent = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        multipartFormContent.Add(streamContent, name: "file", remoteFileName);
        using var response = await httpClient.PostAsync(
            "https://localhost:7268/upload", multipartFormContent);
        response.EnsureSuccessStatusCode();
    }
}