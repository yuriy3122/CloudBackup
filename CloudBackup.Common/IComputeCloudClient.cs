using Newtonsoft.Json;
/// <summary>
/// https://cloud..com/en-ru/docs/compute/api-ref/
/// </summary>
namespace CloudBackup.Common
{
    public class IamTokenResult
    {
        public string? IamToken { get; set; }
        public string? ExpiresAt { get; set; }
    }

    public class DiskSnapshot
    {
        public List<string>? ProductIds { get; set; }
        public string? Id { get; set; }
        public string? FolderId { get; set; }
        public string? CreatedAt { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? StorageSize { get; set; }
        public string? DiskSize { get; set; }
        public string? Status { get; set; }
        public string? SourceDiskId { get; set; }
    }

    public class DiskSnapshotList
    {
        public List<DiskSnapshot>? Snapshots { get; set; }
        public string? NextPageToken { get; set; }
    }

    public class InstanceResources
    {
        public string? Memory { get; set; }
        public string? Cores { get; set; }
        public string? CoreFraction { get; set; }
        public string? Gpus { get; set; }
    }

    public class Disk
    {
        public string? Mode { get; set; }
        public string? DeviceName { get; set; }
        public bool AutoDelete { get; set; }
        public string? DiskId { get; set; }
    }

    public class DiskPlacementPolicy
    {
        public string? PlacementGroupId { get; set; }
    }

    public class DiskDescription
    {
        public string? Id { get; set; }
        public string? FolderId { get; set; }
        public string? CreatedAt { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public object? Labels { get; set; }
        public string? TypeId { get; set; }
        public string? ZoneId { get; set; }
        public string? Size { get; set; }
        public string? BlockSize { get; set; }
        public List<string>? ProductIds { get; set; }
        public string? Status { get; set; }
        public List<string>? InstanceIds { get; set; }
        public DiskPlacementPolicy? DiskPlacementPolicy { get; set; }
        public string? SourceImageId { get; set; }
        public string? SourceSnapshotId { get; set; }
    }

    public class CreateDiskRequest
    {
        public string FolderId { get; set; } = null!;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? TypeId { get; set; }
        public string? ZoneId { get; set; }
        public long Size { get; set; }
        public long BlockSize { get; set; }
        public string? SnapshotId { get; set; }
    }

    public class CreateResourceResponseError
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
        public List<object>? Details { get; set; }
    }

    public class CreateResourceResponse
    {
        public string? Id { get; set; }
        public string? Description { get; set; }
        public string? CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedAt { get; set; }
        public bool Done { get; set; }
        public object? Metadata { get; set; }
        public object? Response { get; set; }
        public CreateResourceResponseError? Error { get; set; }
    }

    public class FileSystem
    {
        public string? Mode { get; set; }
        public string? DeviceName { get; set; }
        public string? FilesystemId { get; set; }
    }

    public class DnsRecord
    {
        public string? Fqdn { get; set; }
        public string? DnsZoneId { get; set; }
        public string? Ttl { get; set; }
        public bool? Ptr { get; set; }
    }

    public class OneToOneNat
    {
        public string? Address { get; set; }
        public string? IpVersion { get; set; }
        public List<DnsRecord>? DnsRecords { get; set; }
    }

    public class IpAddress
    {
        public string? Address { get; set; }
        public OneToOneNat? OneToOneNat { get; set; }
        public List<DnsRecord>? DnsRecords { get; set; }
    }

    public class NetworkInterface
    {
        public string? Index { get; set; }
        public string? MacAddress { get; set; }
        public string? SubnetId { get; set; }
        public IpAddress? PrimaryV4Address { get; set; }
        public IpAddress? PrimaryV6Address { get; set; }
    }

    public class SchedulingPolicy
    {
        public bool Preemptible { get; set; }
    }

    public class NetworkSettings
    {
        public string? Type { get; set; }
    }

    public class HostAffinityRule
    {
        public string? Key { get; set; }
        public string? Op { get; set; }
        public List<string>? Values { get; set; }
    }

    public class PlacementPolicy
    {
        public string? PlacementGroupId { get; set; }
        public List<HostAffinityRule>? HostAffinityRules { get; set; }
    }

    public class Instance
    {
        public string? Id { get; set; }
        public string? FolderId { get; set; }
        public string? CreatedAt { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Labels { get; set; }
        public string? ZoneId { get; set; }
        public string? PlatformId { get; set; }
        public InstanceResources? Resources { get; set; }
        public string? Status { get; set; }
        public string? Metadata { get; set; }
        public Disk? BootDisk { get; set; }
        public List<Disk>? SecondaryDisks { get; set; }
        public List<Disk>? LocalDisks { get; set; }
        public List<FileSystem>? FileSystems { get; set; }
        public List<NetworkInterface>? NetworkInterfaces { get; set; }
        public List<string>? SecurityGroupIds { get; set; }
        public string? Fqdn { get; set; }
        public string? ServiceAccountId { get; set; }
        public NetworkSettings? NetworkSettings { get; set; }
        public PlacementPolicy? PlacementPolicy { get; set; }
    }

    public class InstanceList
    {
        public List<Instance>? Instances { get; set; }
        public string? NextPageToken { get; set; }
    }

    public class DiskDescriptionList
    {
        public List<DiskDescription>? Disks { get; set; }
        public string? NextPageToken { get; set; }
    }

    public class DiskSpecification
    {
        public string? Mode { get; set; }
        public string? DeviceName { get; set; }
        public bool AutoDelete { get; set; }
        public string? DiskId { get; set; }
        public DiskParams? DiskSpec { get; set; }
    }

    public class DiskCreateInstanceSpecification
    {
        public string? DiskId { get; set; }
    }

    public class DiskParams
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? TypeId { get; set; }
        public string? Size { get; set; }
        public string? BlockSize { get; set; }
        public DiskPlacementPolicy? DiskPlacementPolicy { get; set; }
        public string? ImageId { get; set; }
        public string? SnapshotId { get; set; }
    }

    public class LocalDiskSpec
    {
        public string? Size { get; set; }
    }

    public class OneToOneNatSpec
    {
        public string? IpVersion { get; set; }
    }

    public class IpAddressSpec
    {
        public OneToOneNatSpec? OneToOneNatSpec { get; set; }
        public List<DnsRecord>? DnsRecordSpecs { get; set; }
    }

    public class NetworkInterfaceSpec
    {
        public string? SubnetId { get; set; }
        public IpAddressSpec? PrimaryV4AddressSpec { get; set; }
        public List<string>? SecurityGroupIds { get; set; }
    }

    public class InstanceResourcesSpec
    {
        public long Memory { get; set; }
        public int Cores { get; set; }
        public long CoreFraction { get; set; }
    }

    public class CreateInstanceMetadata
    {
        [JsonProperty(PropertyName = "user-data")]
        public string? Userdata { get; set; }
    }

    public class CreateInstanceRequest
    {
        public DiskCreateInstanceSpecification? BootDiskSpec { get; set; }
        public string? Description { get; set; }
        public List<FileSystem>? FilesystemSpecs { get; set; }
        public string? FolderId { get; set; }
        public string? Hostname { get; set; }
        public CreateInstanceMetadata? Metadata { get; set; }
        public string? Name { get; set; }
        public List<NetworkInterfaceSpec>? NetworkInterfaceSpecs { get; set; }
        public object? PlacementPolicy { get; set; }
        public string? PlatformId { get; set; }
        public InstanceResourcesSpec? ResourcesSpec { get; set; }
        public List<DiskSpecification>? SecondaryDiskSpecs { get; set; }
        public string? ZoneId { get; set; }
    }

    public class DeleteSnapshotResponse
    {
        public string? Id { get; set; }
        public string? Description { get; set; }
        public string? CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public bool Done { get; set; }
        public object? Metadata { get; set; }
        public CreateResourceResponseError? Error { get; set; }
    }

    public class AttachDiskRequest
    {
        public string? Mode { get; set; }
        public string? DeviceName { get; set; }
        public bool AutoDelete { get; set; }
        public DiskParams? DiskSpec { get; set; }
        public string? DiskId { get; set; }
    }

    public class DetachDiskRequest
    {
        public string? DiskId { get; set; }

        public string? DeviceName { get; set; }
    }

    public class CommonResponse
    {
        public string? Id { get; set; }
        public string? Description { get; set; }
        public string? CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedAt { get; set; }
        public bool Done { get; set; }
        public object? Metadata { get; set; }
        public CreateResourceResponseError? Error { get; set; }
        public object? Response { get; set; }
    }

    public class AttachDiskResponse : CommonResponse
    {
    }

    public class DetachDiskResponse : CommonResponse
    {
    }

    public class DeleteDiskResponse : CommonResponse
    {
    }

    public class StartInstanceResponse : CommonResponse
    {
    }

    public class StopInstanceResponse : CommonResponse
    {
    }

    public class RestartInstanceResponse : CommonResponse
    {
    }

    public class DeleteInstanceResponse : CommonResponse
    {
    }

    public interface IComputeCloudClient
    {
        Task<InstanceList?> GetInstanceList(string folderId);

        Task<DiskSnapshot?> CreateSnapshot(string folderId, string diskId);

        Task DeleteSnapshot(string snapshotId);

        Task<DiskDescriptionList?> GetDiskList(string folderId);

        Task<CreateResourceResponse?> CreateDisk(CreateDiskRequest request);

        Task<CreateResourceResponse?> CreateInstance(CreateInstanceRequest request);

        Task<AttachDiskResponse?> AttachDisk(string instanceId, string folderId, AttachDiskRequest request);

        Task<DetachDiskResponse?> DetachDisk(string instanceId, string folderId, DetachDiskRequest request);

        Task<DeleteDiskResponse?> DeleteDisk(string diskId);

        Task StartInstances(string folderId, string[] instanceIds);

        Task StopInstances(string folderId, string[] instanceIds);

        Task RestartInstances(string folderId, string[] instanceIds);

        Task DeleteInstances(string[] instanceIds);
    }
}
