using CloudBackup.Model;

namespace CloudBackup.Repositories
{
    public class ProfileRepository : RepositoryBase<Profile>
    {
        public ProfileRepository(BackupContext context) : base(context)
        {
        }
    }
}