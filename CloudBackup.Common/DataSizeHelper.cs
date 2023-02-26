
namespace CloudBackup.Common
{
    public static class DataSizeHelper
    {
        public static string GetFormattedDataSize(long size)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = size;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            string result = $"{len:0.##} {sizes[order]}";

            return result;
        }
    }
}
