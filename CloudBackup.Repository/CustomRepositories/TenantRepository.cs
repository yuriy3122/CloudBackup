using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class TenantRepository : RepositoryBase<Tenant>
    {
        public TenantRepository(BackupContext context) : base(context)
        {
        }
    }
}