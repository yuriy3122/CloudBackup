
namespace CloudBackup.Model
{
    public class JobConfiguration : EntityBase
    {
        public Job Job { get; set; } = null!;

        public int JobId { get; set; }

        public string Configuration { get; set; } = null!;
    }
}