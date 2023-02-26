
namespace CloudBackup.Common
{
    public enum VolumeRestoreMode
    {
        //Volume attached to newly created instance
        Default = 0,

        //Standalone volume restore
        Standalone = 1,

        //Attach volume only if selected device if free, otherwise throw exception
        AttachToInstanceOnlyIfDeviceIsFree = 2,

        //Switch attached volume. Instance must be in stopped state before attachment
        SwitchAttachedVolume = 3,

        //Switch attached volume, delete old volume after new volume attached. Instance must be in stopped state before attachment
        SwitchAttachedVolumeAndDeleteOld = 4
    }

    public class RestoreOptions
    {
        public string? DestPlacement { get; set; }

        public int? DestProfileId { get; set; }

        #region Instance

        public int InstanceCount { get; set; }

        public string? InstanceId { get; set; }

        public string? InstancePassword { get; set; }

        public List<InstanceVolumeRestoreOptions>? InstanceVolumeRestoreOptions { get; set; }

        #endregion

        #region Volumes

        public VolumeRestoreMode? VolumeRestoreMode { get; set; }

        public string? VolumeId { get; set; }

        public string? FolderId { get; set; }

        #endregion

        public RestoreOptions()
        {
            DestPlacement = string.Empty;
            DestProfileId = 0;
            InstanceVolumeRestoreOptions = new List<InstanceVolumeRestoreOptions>();
        }
    }

    public class InstanceVolumeRestoreOptions
    {
        public string? VolumeId { get; set; }

        public bool Exclude { get; set; }
    }
}