using System.Diagnostics;
using FileDownloader;

var sw = Stopwatch.StartNew();

var progress = new Progress<double>(p =>
{
    Console.WriteLine(Math.Round(p, 2));
});
await FileDownloaderPV113.DownloadFile(
    "https://github.com/rodion-m/SystemProgrammingCourse2022/raw/master/files/payments_19mb.zip",
    progress
);

Console.WriteLine($"Done: {sw.ElapsedMilliseconds}");
















// var sw = Stopwatch.StartNew();
// await SmartDownloader.DownloadFileAsync(
//     "https://github.com/rodion-m/SystemProgrammingCourse2022/raw/master/files/payments_270mb.zip", 
//     "file4.zip",
//     progress: new Progress<SmartDownloader.LoadingProgress>(p =>
//     {
//         Console.WriteLine(p.TotalSize);
//         Console.WriteLine(p.DownloadedBytes);
//         Console.WriteLine(p.Progress);
//     })
// );
//
// Console.WriteLine($"Done: {sw.ElapsedMilliseconds}");