
namespace CloudBackup.Common
{
    public interface IComputeCloudClientFactory
    {
        IComputeCloudClient CreateComputeCloudClient(CloudCredentials credentials);
    }

    public class ComputeCloudClientFactory : IComputeCloudClientFactory
    {
        public IComputeCloudClient CreateComputeCloudClient(CloudCredentials credentials)
        {
            return new ComputeCloudClient(credentials);
        }
    }
}