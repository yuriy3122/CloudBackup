using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using CloudBackup.Model;
using CloudBackup.Repositories;
using CloudBackup.Common;

namespace CloudBackup.Services
{
    public interface INotificationService
    {
        Task Send(int tenantId, NotificationType type, string subject, string message);
    }

    public class NotificationService : INotificationService
    {
        private readonly IRepository<NotificationConfiguration> _notificationConfigurationRepository;

        public NotificationService(IRepository<NotificationConfiguration> notificationConfigurationRepository)
        {
            _notificationConfigurationRepository = notificationConfigurationRepository;
        }

        public async Task Send(int tenantId, NotificationType type, string subject, string message)
        {
            static IQueryable<NotificationConfiguration> Includes(IQueryable<NotificationConfiguration> i) => 
                i.Include(p => p.NotificationDeliveryConfiguration);

            var configurations = (await _notificationConfigurationRepository.FindAsync(f => f.TenantId == tenantId &&
                f.Type == type, null, null, Includes)).ToList();

            foreach (var configuration in configurations)
            {
                var deliveryConfig = configuration.NotificationDeliveryConfiguration;

                if (deliveryConfig == null)
                {
                    continue;
                }

                if (deliveryConfig.DeliveryMethod == DeliveryMethod.Smtp)
                {
                    var smtpConfig = JsonConvert.DeserializeObject<SmtpServerConfiguration>(deliveryConfig.Configuration);

                    if (smtpConfig == null)
                    {
                        var errorMessage = "SmtpServerConfiguration is empty";
                        throw new ArgumentNullException(errorMessage);
                    }

                    var mimeMessage = new MimeMessage();
                    mimeMessage.From.Add(new MailboxAddress(" Backup", smtpConfig.SenderEmail));
                    mimeMessage.To.Add(new MailboxAddress(configuration.Email!.Split('@').First(), configuration.Email));
                    mimeMessage.Subject = subject;
                    mimeMessage.Body = new TextPart("plain") { Text = message};

                    using var client = new SmtpClient();

                    client.Connect(smtpConfig.Host, smtpConfig.Port, SecureSocketOptions.Auto);
                    client.Authenticate(smtpConfig.UserName, smtpConfig.Password);
                    client.Send(mimeMessage);
                    client.Disconnect(true);
                }
            }
        }
    }
}
