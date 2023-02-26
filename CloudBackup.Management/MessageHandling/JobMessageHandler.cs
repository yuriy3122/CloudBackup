using System.Net.WebSockets;
using Microsoft.EntityFrameworkCore;
using CloudBackup.Management.WebSockets;
using CloudBackup.Repositories;
using CloudBackup.Model;

namespace CloudBackup.Management.MessageHandling
{
    public class JobMessageHandler : WebSocketHandler
    {
        private volatile bool _started;
        private List<ulong>? _jobRows;
        private List<ulong>? _restoreJobRows;
        public List<string>? ErrorList { get; set; }

        private readonly IConfiguration _configuration;

        public JobMessageHandler(WebSocketConnectionManager webSocketConnectionManager,
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

                var tasks = new List<Task>
                {
                    ProcessJobChanges(),
                    ProcessRestoreJobChanges()
                };

                await Task.WhenAll(tasks);
            }
        }

        private DbContextOptions<BackupContext> GetDbContextOption()
        {
            var optionsBuilder = new DbContextOptionsBuilder<BackupContext>();

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return optionsBuilder.Options;
        }

        private async Task ProcessRestoreJobChanges()
        {
            while (true)
            {
                try
                {
                    var restoreJobs = new List<RestoreJob>();

                    using (var context = new BackupContext(GetDbContextOption()))
                    {
                        restoreJobs = await context.RestoreJob.ToListAsync();
                    }

                    var rows = restoreJobs.Any() ? restoreJobs.Select(x => BitConverter.ToUInt64(x.RowVersion, 0)).ToList() : new List<ulong>();

                    if (_restoreJobRows == null)
                    {
                        _restoreJobRows = rows;
                    }
                    else
                    {
                        if (!_restoreJobRows.SequenceEqual(rows))
                        {
                            await SendMessageToAllAsync("RestoreJobList");
                        }

                        _restoreJobRows = rows;
                    }
                }
                catch (Exception ex)
                {
                    ErrorList?.Add(ex.Message);
                }

                await Task.Delay(1000);
            }
        }

        private async Task ProcessJobChanges()
        {
            while (true)
            {
                try
                {
                    var jobs = new List<Job>();

                    using (var context = new BackupContext(GetDbContextOption()))
                    {
                        jobs = await context.Jobs.ToListAsync();
                    }

                    var rows = jobs.Any() ? jobs.Select(x => BitConverter.ToUInt64(x.RowVersion, 0)).ToList() : new List<ulong>();

                    if (_jobRows == null)
                    {
                        _jobRows = rows;
                    }
                    else
                    {
                        if (!_jobRows.SequenceEqual(rows))
                        {
                            await SendMessageToAllAsync("JobList");
                        }

                        _jobRows = rows;
                    }
                }
                catch (Exception ex)
                {
                    ErrorList?.Add(ex.Message);
                }

                await Task.Delay(1000);
            }
        }
    }
}
