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
                    HttpClient webClient = new HttpClient();
                    string filePath = Path.Combine(resultPath, chunkedFile.File);
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (File.Exists(filePath) && fileInfo.Length == chunkedFile.FileSize)
                    {
                        completedBytes += chunkedFile.FileSize;
                        semaphore.Release();
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                        using (FileStream outputStream = File.OpenWrite(filePath))
                        {
                            foreach (int chunkId in chunkedFile.ChunksIds)
                            {
                                try
                                {
                                    string url = $"https://manifest.simplyblk.xyz/{version}/{chunkId}.chunk";
                                    byte[] chunkData = await webClient.GetByteArrayAsync(url);
                                    byte[] chunkDecompData = new byte[67108865];
                                    using MemoryStream memoryStream = new (chunkData);
                                    using (GZipStream decompressionStream = new (memoryStream, CompressionMode.Decompress))
                                    {
                                        int bytesRead;
                                        while ((bytesRead = await decompressionStream.ReadAsync(chunkDecompData, 0, chunkDecompData.Length)) > 0)
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
                                    }
                                }
                                catch (Exception)
                                {
                                }
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