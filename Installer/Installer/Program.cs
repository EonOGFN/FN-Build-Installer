using Newtonsoft.Json;
using System.Net;
using static FNBuildInstaller.Installer.FileManifest;
using static FNBuildInstaller.OGFN.Project;
using FNBuildInstaller.OGFN;

namespace FNBuildInstaller.Installer
{
    class Program
    {
        private static async Task<List<string>> GetVersionsAsync()
        {
            string versionsJson = await Globals.httpClient.GetStringAsync("https://manifest.simplyblk.xyz/versions.json");
            return !string.IsNullOrEmpty(versionsJson) ? JsonConvert.DeserializeObject<List<string>>(versionsJson) ?? throw new Exception("failed to parse versions") : throw new Exception("failed to get versions");
        }

        private static async Task<ManifestFile> GetManifestAsync(string version)
        {
            string manifestUrl = $"https://manifest.simplyblk.xyz/{version}/{version}.manifest";
            string manifestJson = await Globals.httpClient.GetStringAsync(manifestUrl);
            return !string.IsNullOrEmpty(manifestJson) ? JsonConvert.DeserializeObject<FileManifest.ManifestFile>(manifestJson) ?? throw new Exception("failed to parse manifest") : throw new Exception("failed to get manifest");
        }

        private static async Task Main(string[] args)
        {
            var httpClient = new WebClient();
            List<string> versions = JsonConvert.DeserializeObject<List<string>>(httpClient.DownloadString(Globals.SeasonBuildVersion + "/versions.json"));

            Console.Clear();

            Console.Title = $"{Project.Name} Build Installer - Version {Project.Build}";
            Console.Write($"You are about to install Fortnite Version {Project.Build}. Do you wish to proceed with this installation");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("?\nPlease type ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("'Yes' ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("or ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("'No'\n");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(">> ");

            string? confirmation = Console.ReadLine();
            if (confirmation?.ToLower() != "yes")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Closing in 5 seconds...");
                Console.Out.Flush();
                Thread.Sleep(5000);
                return;
            }

            const int versionIndex = Project.FNBuild;
            if (versionIndex >= 0 && versionIndex < versions.Count)
            {
                string targetVersion = versions[versionIndex].Split("-", StringSplitOptions.None)[1];
                ManifestFile manifest = JsonConvert.DeserializeObject<ManifestFile>(httpClient.DownloadString(Globals.SeasonBuildVersion + $"/{targetVersion}/{targetVersion}.manifest"));

                Console.Write("Please enter the installation path for the game: ");
                string? resultPath = Console.ReadLine();
                if (!string.IsNullOrEmpty(resultPath))
                {
                    await Installer.Download(manifest, targetVersion, resultPath);
                }
                else
                {
                    Console.WriteLine("Invalid path. Exiting...");
                    return;
                }
            }
            else
            {
                Console.WriteLine("Selected version is out of range. Exiting...");
                return;
            }
        }
    }
}