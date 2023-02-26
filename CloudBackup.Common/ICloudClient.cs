
namespace CloudBackup.Common
{
    public class Cloud
    {
        public string? Id { get; set; }
        public string? CreatedAt { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? OrganizationId { get; set; }
        public string? Labels { get; set; }
    }

    public class CloudList
    {
        public List<Cloud>? Clouds { get; set; }
    }

    public class CloudFolder
    {
        public string? Id { get; set; }
        public string? CloudId { get; set; }
        public string? CreatedAt { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Labels { get; set; }
        public string? Status { get; set; }
    }

    public class CloudFolderList
    {
        public List<CloudFolder>? Folders { get; set; }
    }

    public interface ICloudClient
    {
        Task<CloudList?> GetCloudList();

        Task<CloudFolderList?> GetCloudFolderList(string cloudId);
    }
}
