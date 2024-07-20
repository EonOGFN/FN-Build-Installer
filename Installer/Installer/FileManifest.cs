namespace FNBuildInstaller.Installer
{
    internal class FileManifest
    {
        public class ChunkedFile
        {
            public List<int> ChunksIds = new List<int>();
            public string File = string.Empty;
            public long FileSize;
        }

        public class ManifestFile
        {
            public string Name = string.Empty;
            public List<ChunkedFile> Chunks = new List<ChunkedFile>();
            public long Size;
        }
    }
}
