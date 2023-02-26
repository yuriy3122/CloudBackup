
namespace CloudBackup.Common
{
    public class SmtpServerConfiguration
    {
        public string? SenderEmail { get; set; }

        public string? Host { get; set; }

        public int Port { get; set; }

        public string? UserName { get; set; }

        public string? Password { get; set; }
    }
}
