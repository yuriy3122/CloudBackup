using System.Net.WebSockets;
using Microsoft.EntityFrameworkCore;
using CloudBackup.Management.WebSockets;
using CloudBackup.Repositories;
using CloudBackup.Model;

namespace CloudBackup.Management.MessageHandling
{
    public class AlertMessageHandler : WebSocketHandler
    {
        private volatile bool _started;
        private DateTime? _lastAlertDate;
        public List<string>? ErrorList { get; set; }

        private readonly IConfiguration _configuration;

        public AlertMessageHandler(WebSocketConnectionManager webSocketConnectionManager,
                                   IConfiguration configuration) : base(webSocketConnectionManager)
        {
            _configuration = configuration;
        }

        public override Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            return Task.Delay(1);
        }

        public override async void OnConnected(WebSocket socket)
        {
            base.OnConnected(socket);

            if (!_started)
            {
                _started = true;
                await ProcessAlertChanges();
            }
        }

        private DbContextOptions<BackupContext> GetDbContextOption()
        {
            var optionsBuilder = new DbContextOptionsBuilder<BackupContext>();

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return optionsBuilder.Options;
        }

        private async Task ProcessAlertChanges()
        {
            while (true)
            {
                try
                {
                    Alert? lastAlert = null;

                    using (var context = new BackupContext(GetDbContextOption()))
                    {
                        var query = context.Alerts.AsQueryable();

                        if (_lastAlertDate != null)
                        {
                            query = query.Where(alert => alert.IsProcessed && alert.Date > _lastAlertDate);
                        }

                        if (query != null)
                        {
                            lastAlert = await query.OrderByDescending(x => x.Date).FirstOrDefaultAsync();
                        }
                    }

                    if (lastAlert != null)
                    {
                        await SendMessageToAllAsync("NewAlerts");

                        _lastAlertDate = lastAlert.Date;
                    }
                }
                catch (Exception ex)
                {
                    if (ErrorList != null)
                    {
                        ErrorList.Add(ex.Message);
                    }
                }

                await Task.Delay(1000);
            }
        }
    }
}
