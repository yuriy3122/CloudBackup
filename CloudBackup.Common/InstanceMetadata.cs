
namespace CloudBackup.Common
{
    public static class InstanceMetadata
    {
        private static readonly object _locker = new();
        private static string? _instanceId;

        public static string GetInstanceId()
        {
            lock (_locker)
            {
                if (_instanceId == null)
                {
#if (DEBUG)
                    _instanceId = "epdin6dcm3kiqfmk7t7d";
#else
                    var command = "curl http://169.254.169.254/latest/meta-data/instance-id";
                    var output = BashCommand.Run(command);

                    if (output.Count != 1)
                    {
                        throw new ArgumentNullException("instance-id is not defined");
                    }

                    _instanceId = output.First().Trim();
#endif
                }
            }

            return _instanceId;
        }
    }
}