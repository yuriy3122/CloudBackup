using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using CloudBackup.Repositories;
using CloudBackup.Model;
using CloudBackup.Services;
using CloudBackup.Common;

namespace CloudBackup.Management.Controllers
{
    [Route("Core/Notifications")]
    public class NotificationController : CommonController
    {
        private readonly ITenantService _tenantService;
        private readonly IRepository<NotificationConfiguration> _notificationConfigurationRepository;
        private readonly IRepository<NotificationDeliveryConfiguration> _notificationDeliveryConfigurationRepository;

        public NotificationController(ITenantService tenantService,
            IRepository<NotificationConfiguration> notificationConfigurationRepository,
            IRepository<NotificationDeliveryConfiguration> notificationDeliveryConfigurationRepository,
            IUserRepository userRepository) : base(userRepository)
        {
            _tenantService = tenantService;
            _notificationConfigurationRepository = notificationConfigurationRepository;
            _notificationDeliveryConfigurationRepository = notificationDeliveryConfigurationRepository;
        }

        [HttpGet]
        [Route("DeliveryConfigurations")]
        public async Task<ActionResult<ModelList<NotificationDeliveryConfigurationViewModel>>> GetNotificationConfigurations()
        {
            var allowedTenantIds = await _tenantService.GetAllowedTenantIds(CurrentUser.Id);

            var configurations = (await _notificationDeliveryConfigurationRepository.FindAsync(
                f => allowedTenantIds.Contains(f.TenantId), null, null, null)).ToList();

            var count = await _notificationDeliveryConfigurationRepository.CountAsync(
                f => allowedTenantIds.Contains(f.TenantId), null);

            var configurationViewModels = new List<NotificationDeliveryConfigurationViewModel>();

            foreach (var configuration in configurations)
            {
                configurationViewModels.Add(new NotificationDeliveryConfigurationViewModel(configuration));
            }

            var notificationsViewModelList = ModelList.Create(configurationViewModels, count);

            return notificationsViewModelList;
        }

        [HttpGet]
        public async Task<ActionResult<ModelList<NotificationConfigurationViewModel>>> GetNotificationConfigurations(
            int? pageSize, int? pageNum, string order)
        {
            var allowedTenantIds = await _tenantService.GetAllowedTenantIds(CurrentUser.Id);

            var configurations = (await _notificationConfigurationRepository.FindAsync(f => allowedTenantIds.Contains(f.TenantId),
                order, new EntitiesPage(pageSize ?? short.MaxValue, pageNum ?? 1), i => i.Include(p => p.NotificationDeliveryConfiguration).Include(p => p.Tenant))).ToList();

            var count = await _notificationConfigurationRepository.CountAsync(f => allowedTenantIds.Contains(f.TenantId), null);

            var notifications = new List<NotificationConfigurationViewModel>();

            foreach (var configuration in configurations)
            {
                notifications.Add(new NotificationConfigurationViewModel(configuration));
            }

            var notificationsViewModelList = ModelList.Create(notifications, count);

            return notificationsViewModelList;
        }

        [HttpPut]
        [Route("{id:int}")]
        public async Task<IActionResult> UpdateNotificationConfiguration(int id, [FromBody] NotificationConfigurationViewModel input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(NotificationConfigurationViewModel));
            }

            var deliveryId = await UpdateDeliveryConfiguration(input);

            var config = await _notificationConfigurationRepository.FindByIdAsync(input.Id, null);

            config.Email = input.Email;
            config.TenantId = input.TenantId;
            config.Name = input.Name;
            config.Type = (NotificationType)input.Type;
            config.IncludeTenants = input.IncludeTenants;
            config.IsEnabled = true;
            config.NotificationDeliveryConfigurationId = deliveryId;

            _notificationConfigurationRepository.Update(config);

            await _notificationConfigurationRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<NotificationConfigurationViewModel>> AddNotificationConfiguration([FromBody] NotificationConfigurationViewModel input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(NotificationConfigurationViewModel));
            }

            var deliveryId = await UpdateDeliveryConfiguration(input);

            var configuration = new NotificationConfiguration
            {
                Email = input.Email,
                TenantId = input.TenantId,
                Name = input.Name,
                Type = (NotificationType)input.Type,
                IncludeTenants = input.IncludeTenants,
                IsEnabled = true,
                NotificationDeliveryConfigurationId = deliveryId
            };

            _notificationConfigurationRepository.Add(configuration);

            await _notificationConfigurationRepository.SaveChangesAsync();

            var newViewModel = new NotificationConfigurationViewModel(configuration);

            return CreatedAtAction("GetConfiguration", new { id = configuration.Id }, newViewModel);
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveNotificationConfigurations(string configIds)
        {
            if (string.IsNullOrEmpty(configIds))
            {
                return NoContent();
            }

            var ids = JsonConvert.DeserializeObject<List<int>>(configIds) ?? new List<int>();

            var configurations = (await _notificationConfigurationRepository.FindAsync(
                f => ids.Contains(f.Id), null, null, null)).ToList();

            foreach (var configuration in configurations)
            {
                _notificationConfigurationRepository.Remove(configuration);
            }

            await _notificationConfigurationRepository.SaveChangesAsync();

            return NoContent();
        }

        private async Task<int> UpdateDeliveryConfiguration(NotificationConfigurationViewModel input)
        {
            var delivery = input.DeliveryConfig!.Id != 0
                ? await _notificationDeliveryConfigurationRepository.FindByIdAsync(input.DeliveryConfig!.Id, null)
                : new NotificationDeliveryConfiguration();

            delivery.Name = input.DeliveryConfig!.Name ?? string.Empty;
            delivery.TenantId = input.DeliveryConfig!.TenantId;
            delivery.DeliveryMethod = DeliveryMethod.Smtp;

            if (delivery.DeliveryMethod == DeliveryMethod.Smtp)
            {
                var smtpConfig = new SmtpServerConfiguration
                {
                    SenderEmail = input.DeliveryConfig!.SenderEmail,
                    Host = input.DeliveryConfig!.EmailSmtpServer,
                    Port = input.DeliveryConfig!.EmailSmtpPort,
                    UserName = input.DeliveryConfig!.EmailSmtpUserName,
                    Password = input.DeliveryConfig!.EmailSmtpUserPassword
                };

                delivery.Configuration = JsonConvert.SerializeObject(smtpConfig);
            }

            if (delivery.Id == 0)
            {
                _notificationDeliveryConfigurationRepository.Add(delivery);
            }
            else
            {
                _notificationDeliveryConfigurationRepository.Update(delivery);
            }

            await _notificationDeliveryConfigurationRepository.SaveChangesAsync();

            return delivery.Id;
        }
    }

    public class NotificationConfigurationViewModel
    {
        public int Id { get; set; }//hidden field
        public string? RowVersion { get; set; }//hidden field
        public string? Name { get; set; }
        public int TenantId { get; set; }
        public string? TenantName { get; set; }
        public string? Email { get; set; }
        public int Type { get; set; }
        public string? TypeStr { get; set; }
        public NotificationDeliveryConfigurationViewModel? DeliveryConfig { get; set; }
        public bool IncludeTenants { get; set; }

        public NotificationConfigurationViewModel()
        {
            Id = 0;
        }

        public NotificationConfigurationViewModel(NotificationConfiguration configuration)
        {
            if (configuration != null)
            {
                Id = configuration.Id;
                RowVersion = configuration.RowVersion != null ? Convert.ToBase64String(configuration.RowVersion) : string.Empty;
                Name = configuration.Name;
                TenantId = configuration.TenantId;
                TenantName = configuration.Tenant?.Name ?? string.Empty;
                IncludeTenants = configuration.IncludeTenants;
                Email = configuration.Email;
                Type = (int)configuration.Type;
                TypeStr = configuration.Type.ToString();

                var dc = configuration.NotificationDeliveryConfiguration;

                if (dc != null)
                {
                    DeliveryConfig = new NotificationDeliveryConfigurationViewModel(dc);
                }
            }
        }
    }

    public class NotificationDeliveryConfigurationViewModel
    {
        public int Id { get; set; }//hidden field
        public string? RowVersion { get; set; }//hidden field
        public string? Name { get; set; }
        public int TenantId { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; }
        public string? SenderEmail { get; set; }
        public string? EmailSmtpServer { get; set; }
        public int EmailSmtpPort { get; set; }
        public string? EmailSmtpUserName { get; set; }
        public string? EmailSmtpUserPassword { get; set; }

        public NotificationDeliveryConfigurationViewModel()
        {
            Id = 0;
        }

        public NotificationDeliveryConfigurationViewModel(NotificationDeliveryConfiguration deliveryConfiguration)
        {
            if (deliveryConfiguration != null)
            {
                Id = deliveryConfiguration.Id;
                RowVersion = deliveryConfiguration.RowVersion != null ? Convert.ToBase64String(deliveryConfiguration.RowVersion) : string.Empty;
                Name = deliveryConfiguration.Name;
                TenantId = deliveryConfiguration.TenantId;
                DeliveryMethod = deliveryConfiguration.DeliveryMethod;

                if (DeliveryMethod == DeliveryMethod.Smtp)
                {
                    var smtpConfig = JsonConvert.DeserializeObject<SmtpServerConfiguration>(
                        deliveryConfiguration.Configuration)!;

                    SenderEmail = smtpConfig.SenderEmail;
                    EmailSmtpServer = smtpConfig.Host;
                    EmailSmtpPort = smtpConfig.Port;
                    EmailSmtpUserName = smtpConfig.UserName;
                    EmailSmtpUserPassword = smtpConfig.Password;
                }
            }
        }
    }
}