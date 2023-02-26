using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class TenantProfileRepository : RepositoryBase<TenantProfile>
    {
        public TenantProfileRepository(BackupContext context) : base(context)
        {
        }
    }
}