using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CloudBackup.Common;
using CloudBackup.Repositories;
using CloudBackup.Services;

namespace CloudBackup.JobProcessorApp
{
    class Program
    {
        private static IConfigurationRoot Configuration { get; set; } = null!;

        static void Main(string[] args)
        {
            var services = new ServiceCollection();

            ConfigureServices(services);

            var serviceProvider = services.BuildServiceProvider();

            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<BackupContext>();
                context?.Database.Migrate();
            }

            serviceProvider.GetService<ConfigurationProcessor>()!.RunAsync().Wait();

            var tasks = new List<Task>
            {
                serviceProvider.GetService<BackupJobProcessor>()!.RunAsync(),
                serviceProvider.GetService<RestoreJobProcessor>()!.RunAsync(),
                serviceProvider.GetService<AlertsProcessor>()!.RunAsync(),
                serviceProvider.GetService<NotificationProcessor>()!.RunAsync()
            };

            Task.WaitAll(tasks.ToArray(), Timeout.Infinite);
        }

        private static void BuildConfiguration()
        {
            var path = Directory.GetCurrentDirectory();

            Configuration = new ConfigurationBuilder()
                .SetBasePath(path)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            BuildConfiguration();

            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContextPool<BackupContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            services.AddScoped<IRepositoryFactory, RepositoryFactory>();
            services.AddScoped<ICloudClientFactory, CloudClientFactory>();
            services.AddScoped<IComputeCloudClientFactory, ComputeCloudClientFactory>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<BackupJobProcessor>();
            services.AddScoped<RestoreJobProcessor>();
            services.AddScoped<AlertsProcessor>();
            services.AddScoped<NotificationProcessor>();
            services.AddScoped<ConfigurationProcessor>();
        }
    }
}