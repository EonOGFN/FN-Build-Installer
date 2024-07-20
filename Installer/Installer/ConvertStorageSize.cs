namespace FNBuildInstaller.Installer
{
    internal class ConvertStorageSize
    {
        public static string FormatBytesWithSuffix(long bytes)
        {
            string[] strArray = new string[5]
            {
                "B",
                "KB",
                "MB",
                "GB",
                "TB"
            };
            double num = (double)bytes;
            int index;
            for (index = 0; index < strArray.Length && bytes >= 1024L; bytes /= 1024L)
            {
                num = (double)bytes / 1024.0;
                ++index;
            }
            return string.Format("{0:0.##} {1}", num, strArray[index]);
        }
    }
}
