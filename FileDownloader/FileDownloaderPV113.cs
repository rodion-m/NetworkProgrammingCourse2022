namespace FileDownloader;

public class FileDownloaderPV113
{
    
    //0 - 1
    public static async Task DownloadFile(string uri, IProgress<double> progress)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var uriResult))
        {
            throw new ArgumentException("Invalid uri", nameof(uri));
        }
        using var client = new HttpClient();
        using HttpResponseMessage response = await client.GetAsync(
            uriResult, HttpCompletionOption.ResponseHeadersRead);
        long? contentLength = response.Content.Headers.ContentLength; //"Content-Length"
        
        await using Stream contentStream = await response.Content.ReadAsStreamAsync();
        byte[] buffer = new byte[8192];
        long totalBytesRead = 0L;
        while (true)
        {
            int countOfBytesRead = await contentStream.ReadAsync(buffer);
            if (countOfBytesRead == 0)
            {
                break;
            }

            if (countOfBytesRead == buffer.Length)
            {
                //записать buffer в файл
            }
            else //НЕ ВСЕ БАЙТЫ БУФЕРА БЫЛИ ЗАПОЛНЕНЫ ПОЛЕЗНОЙ НАГРУЗКОЙ
            {
                byte[] bytesRead = buffer[..countOfBytesRead];
                //записать bytesRead в файл
            }
            totalBytesRead += countOfBytesRead;
            
            if (contentLength != null && totalBytesRead % 10 == 0)
            {
                progress.Report((double)totalBytesRead / contentLength.Value);
            }
        }
    }
}