using System;
using System.Text;

namespace CloudBackup.Common
{
    public static class StringExtensions
    {
        public static string Base64Encode(this string decodedData)
        {
            var decodedDataBytes = Encoding.UTF8.GetBytes(decodedData);
            var base64EncodedData = Convert.ToBase64String(decodedDataBytes);
            return base64EncodedData;
        }

        public static string Base64Decode(this string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            var decodedData = Encoding.UTF8.GetString(base64EncodedBytes);
            return decodedData;
        }

        public static string TruncateString(this string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return str.Substring(0, Math.Min(str.Length, maxLength));
        }
    }
}
