using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class ConfigurationRepository : RepositoryBase<Configuration>
    {
        public ConfigurationRepository(BackupContext context) : base (context)
        {
        }
    }
}