using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CloudBackup.Repositories;
using CloudBackup.Model;

namespace CloudBackup.Management.Controllers
{
    [Route("Core/Configuration")]
    public class ConfigurationController : Controller
    {
        private readonly IRepository<Configuration> _configurationRepository;

        public ConfigurationController(IRepository<Configuration> configurationRepository)
        {
            _configurationRepository = configurationRepository;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task AddConfiguration([FromBody] ConfigurationViewModel config)
        {
            var currentInstanceId = string.Empty;
#if (DEBUG)
            currentInstanceId = "epdin6dcm3kiqfmk7t7d";
#else
            currentInstanceId = InstanceMetadata.GetInstanceId();
#endif
            if (string.IsNullOrEmpty(config?.UserName))
            {
                throw new ArgumentException("UserName is empty");
            }
            if (string.IsNullOrEmpty(config?.Password))
            {
                throw new ArgumentException("Password is empty");
            }
            if (config?.InstanceId != currentInstanceId)
            {
                throw new ArgumentException("InstanceId do not match current instance");
            }

            var processedConfigurations = await _configurationRepository.FindAsync(f => f.ConfigurationStatus == ConfigurationStatus.Processed, null, null, null);

            if (processedConfigurations.Any())
            {
                return;
            }

            var failedConfigurations = await _configurationRepository.FindAsync(f => f.ConfigurationStatus == ConfigurationStatus.Failed, null, null, null);

            foreach (var failedConfiguration in failedConfigurations)
            {
                _configurationRepository.Remove(failedConfiguration);
            }

            await _configurationRepository.SaveChangesAsync();

            var configurations = await _configurationRepository.FindAllAsync(null);

            if (!configurations.Any())
            {
                _configurationRepository.Add(new Configuration
                {
                    InstanceId = config.InstanceId,
                    UserName = config.UserName,
                    Email = config.Email,
                    Password = config.Password,
                    UtcOffsetTicks = config.UtcOffset.Ticks,
                    ConfigurationStatus = ConfigurationStatus.Created
                });

                await _configurationRepository.SaveChangesAsync();
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("Configured")]
        public async Task<ActionResult<ConfigurationResultViewModel>> IsConfigured()
        {
            var result = new ConfigurationResultViewModel();

            try
            {
                var configurations = await _configurationRepository.FindAllAsync(null);
                var configuration = configurations.FirstOrDefault();

                if (configuration != null)
                {
                    result.IsConfigured = configuration.ConfigurationStatus == ConfigurationStatus.Processed;
                    result.ErrorMessage = configuration.ErrorMessage ?? string.Empty;
                }
            }
            catch
            {
                result.IsConfigured = false;
            }

            return result;
        }
    }

    public class ConfigurationResultViewModel
    {
        public ConfigurationResultViewModel()
        {
            ErrorMessage = string.Empty;
        }

        public bool IsConfigured { get; set; }

        public string? ErrorMessage { get; set; }
    }

    public class ConfigurationViewModel
    {
        public string InstanceId { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;

        public TimeSpan UtcOffset { get; set; }

        public string ConfigurationStatus { get; set; } = null!;
    }
}