
namespace CloudBackup.Common
{
    public class BackupOptions
    {
        public string? Device { get; set; } = null!;

        public DiskDescription Disk { get; set; } = null!;

        public Instance Instance { get; set; } = null!;

        public BackupOptions()
        {
            Device = string.Empty;
            Disk = new DiskDescription();
            Instance = new Instance();
        }

        public BackupOptions(DiskDescription disk)
        {
            Disk = disk;
            Instance = new Instance();
        }

        public BackupOptions(DiskDescription disk, Instance instance) : this(disk)
        {
            Instance = instance;
            Disk = disk;
            Device = instance.BootDisk?.DeviceName;
        }
    }
}