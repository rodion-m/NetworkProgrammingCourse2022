﻿using System.Text;
using System;

namespace FileDownloader;

public static class Downloader
{
    public static async Task DownloadFileAtOnceAsync(string uri, string outputPath)
    {
        using var httpClient = new HttpClient();
        byte[] fileBytes = await httpClient.GetByteArrayAsync(uri);
        await File.WriteAllBytesAsync(outputPath, fileBytes);
    }

    public static async Task DownloadFileAsync(string uri, string outputPath)
    {
        await using var fileStream = File.OpenWrite(outputPath);
        //HttpClient.Dispose() закрывает соединение в коннекшн пуле (в SocketsHttpHandler)
        using var httpClient = new HttpClient();
        //Dispose() освобождаeт соединение для переиспользования
        using var response = await httpClient.GetAsync(
            uri, HttpCompletionOption.ResponseHeadersRead);
        //Dispose() освобождаeт соединение для переиспользования
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        byte[] buffer = new byte[8192 * 2];
        while (true)
        {
            int bytesRead = await contentStream.ReadAsync(buffer);
            if (bytesRead == 0)
            {
                break;
            }
            Console.WriteLine(bytesRead);

            if (bytesRead == buffer.Length)
            {
                await fileStream.WriteAsync(buffer);
            }
            else
            {
                await fileStream.WriteAsync(buffer.Take(bytesRead).ToArray());
            }
        }
    }
    
}


//            var contentLength = response.Content.Headers.ContentLength;