using System.IO.Compression;
using System.Net;

namespace FNBuildInstaller.Installer
{
    internal class Installer
    {
        public static async Task Download(
          FileManifest.ManifestFile manifest,
          string version,
          string resultPath)
        {
            long totalBytes = manifest.Size;
            long completedBytes = 0;
            int progressLength = 0;
            if (!Directory.Exists(resultPath))
                Directory.CreateDirectory(resultPath);
            SemaphoreSlim semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2);
            await Task.WhenAll(Enumerable.Select<FileManifest.ChunkedFile, Task>((IEnumerable<FileManifest.ChunkedFile>)manifest.Chunks, async (chunkedFile) =>
            {
                await semaphore.WaitAsync();
                try
                {
                    WebClient webClient = new WebClient();
                    string filePath = Path.Combine(resultPath, chunkedFile.File);
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (File.Exists(filePath) && fileInfo.Length == chunkedFile.FileSize)
                    {
                        completedBytes += chunkedFile.FileSize;
                        semaphore.Release();
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        using (FileStream outputStream = File.OpenWrite(filePath))
                        {
                            List<int>.Enumerator enumerator = chunkedFile.ChunksIds.GetEnumerator();
                            try
                            {
                                while (enumerator.MoveNext())
                                {
                                    int chunkId = enumerator.Current;
                                    while (true)
                                    {
                                        try
                                        {
                                            string url = $"https://manifest.simplyblk.xyz/{version}/{chunkId}.chunk";
                                            byte[] chunkData = await webClient.DownloadDataTaskAsync(url);
                                            byte[] chunkDecompData = new byte[67108865];
                                            using (MemoryStream memoryStream = new MemoryStream(chunkData))
                                            using (GZipStream decompressionStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                                            {
                                                while (true)
                                                {
                                                    int bytesRead = await decompressionStream.ReadAsync(chunkDecompData, 0, chunkDecompData.Length);
                                                    if (bytesRead > 0)
                                                    {
                                                        await outputStream.WriteAsync(chunkDecompData, 0, bytesRead);
                                                        Interlocked.Add(ref completedBytes, bytesRead);
                                                        double progress = (double)completedBytes / totalBytes * 100.0;
                                                        string progressText = $"\rDownloaded: {ConvertStorageSize.FormatBytesWithSuffix(completedBytes)} / {ConvertStorageSize.FormatBytesWithSuffix(totalBytes)} ({progress:F2}%)";
                                                        int count = progressLength - progressText.Length;
                                                        if (count > 0)
                                                            progressText += new string(' ', count);
                                                        Console.Write(progressText);
                                                        progressLength = progressText.Length;
                                                    }
                                                    else
                                                        break;
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                enumerator.Dispose();
                            }
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
            Console.WriteLine("\n\nFinished Downloading.\nPress any key to exit!");
            Thread.Sleep(100);
            Console.ReadKey();
        }
    }
}