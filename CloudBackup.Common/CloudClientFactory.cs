
namespace CloudBackup.Common
{
    public interface ICloudClientFactory
    {
        ICloudClient CreateCloudClient(CloudCredentials credentials);
    }

    public class CloudClientFactory : ICloudClientFactory
    {
        public ICloudClient CreateCloudClient(CloudCredentials credentials)
        {
            return new CloudClient(credentials);
        }
    }
}