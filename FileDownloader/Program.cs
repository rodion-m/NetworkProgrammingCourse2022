using System.Diagnostics;
using FileDownloader;

var sw = Stopwatch.StartNew();
await SmartDownloader.DownloadFileAsync(
    "https://github.com/rodion-m/SystemProgrammingCourse2022/raw/master/files/payments_270mb.zip", 
    "file4.zip",
    progress: new Progress<SmartDownloader.LoadingProgress>(p =>
    {
        Console.WriteLine(p.TotalSize);
        Console.WriteLine(p.DownloadedBytes);
        Console.WriteLine(p.Progress);
    })
);

Console.WriteLine($"Done: {sw.ElapsedMilliseconds}");