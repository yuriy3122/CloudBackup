using System.Text.RegularExpressions;

namespace CloudBackup.Common
{
    public static class RegexExpressions
    {
        public static string Email => @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
        public static Regex EmailRegex { get; } = new Regex(Email);

        public static string Phone => @"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$";
        public static Regex PhoneRegex { get; } = new Regex(Phone);

        public static string LettersNumbersUnderscores => @"^[a-zA-Z0-9_]*$";
        public static Regex LettersNumbersUnderscoresRegex { get; } = new Regex(LettersNumbersUnderscores);
    }
}
