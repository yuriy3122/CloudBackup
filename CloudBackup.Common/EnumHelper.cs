
namespace CloudBackup.Common
{
    public static class EnumHelper
    {
        public static IEnumerable<T> GetValues<T>() where T: Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static string GetEnumDescription(this Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());

            var attributes = (DescriptionAttribute[]?)fi?.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return (attributes != null && attributes.Length > 0) ? attributes[0].Description : value.ToString();
        }

        public static bool In<T>(this T value, params T[] testValues) where T : Enum
        {
            return testValues.Contains(value);
        }
    }
}